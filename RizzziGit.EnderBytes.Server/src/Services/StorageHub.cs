using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;
using Framework.Collections;

public enum StorageHubType : byte { Blob }

[Flags]
public enum StorageHubFlags : byte { Internal }

public sealed partial class StorageHubService(Server server) : Server.SubService(server, "File systems")
{
  public IMongoDatabase MainDatabase => Server.MainDatabase;
  public IMongoCollection<Record.StorageHub> HubRecords => MainDatabase.GetCollection<Record.StorageHub>();

  private readonly WeakDictionary<long, Hub> Hubs = [];
  private readonly WaitQueue<(TaskCompletionSource<Hub> source, Record.StorageHub storageHub, KeyService.Transformer.Key inputKey)> WaitQueue = new(0);

  public async Task<Hub> Get(Record.StorageHub storageHub, KeyService.Transformer.Key inputKey, CancellationToken cancellationToken)
  {
    Record.Key? key = await Server.KeyService.Keys.FindOneAsync((key) => key.SharedId == inputKey.SharedId, cancellationToken: cancellationToken);
    if (key?.SharedId != inputKey.SharedId)
    {
      throw new ArgumentException("Invalid input key share id.", nameof(inputKey));
    }

    TaskCompletionSource<Hub> source = new();
    using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken,
      GetCancellationToken()
    );

    await WaitQueue.Enqueue((source, storageHub, inputKey), cancellationTokenSource.Token);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, record, inputKey) in WaitQueue)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!Hubs.TryGetValue(record.Id, out Hub? hub))
      {
        hub = Hubs[record.Id] = Hub.StartHub(this, record, inputKey, cancellationToken);
      }

      source.SetResult(hub);
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    IMongoCollection<Record.BlobStorageFileDataMapper> fileDataMappers = MainDatabase.GetCollection<Record.BlobStorageFileDataMapper>();
    IMongoCollection<Record.BlobStorageFileData> fileData = MainDatabase.GetCollection<Record.BlobStorageFileData>();

    fileDataMappers.Watch(async (change, cancellationToken) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (await (await fileDataMappers.FindAsync((fileDataMapper) => fileDataMapper.DataId == change.FullDocument.DataId, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken) != null)
      {
        return;
      }

      await fileData.DeleteManyAsync((fileData) => fileData.Id == change.FullDocument.DataId, cancellationToken);
    }, cancellationToken);

    Server.UserService.Users.Watch(async (change, cancellationToken) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (Hubs.TryGetValue(change.FullDocument.Id, out Hub? storageHub))
      {
        storageHub.Stop();
      }

      await HubRecords.DeleteManyAsync((hub) => hub.OwnerUserId == change.FullDocument.Id, cancellationToken);
    }, cancellationToken);

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

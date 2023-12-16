using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Collections;
using Utilities;

public enum StorageHubType : byte { Blob }

[Flags]
public enum StorageHubFlags : byte { Internal }

public sealed partial class StorageHubService(Server server) : Server.SubService(server, "File systems")
{
  public IMongoCollection<Record.StorageHub> HubRecords => Server.GetCollection<Record.StorageHub>();

  private readonly WeakDictionary<long, Hub> Hubs = [];
  private readonly WaitQueue<(TaskCompletionSource<Hub> source, Record.StorageHub storageHub, KeyGeneratorService.Transformer.Key inputKey)> WaitQueue = new(0);

  public async Task<Hub> Get(long hubId, KeyGeneratorService.Transformer.Key inputKey, CancellationToken cancellationToken)
  {
    Record.StorageHub storageHub = (from record in HubRecords.AsQueryable() where record.Id == hubId select record).FirstOrDefault() ?? throw new ArgumentException("Invalid storage hub id.", nameof(hubId));
    Record.Key key = (from record in Server.KeyGeneratorService.KeyRecords.AsQueryable() where record.SharedId == storageHub.KeySharedId select record).FirstOrDefault() ?? throw new InvalidDataException("Invalid key shared id stored in storage pool.");

    if (key.SharedId != inputKey.SharedId)
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
        hub = Hubs[record.Id] = record.Type switch
        {
          StorageHubType.Blob => new Hub.Blob(this, record.Id, inputKey),

          _ => throw new InvalidDataException("Unknown storage hub type.")
        };

        hub.Start(cancellationToken);
      }

      source.SetResult(hub);
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    Server.UserService.UserRecords.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (Hubs.TryGetValue(change.FullDocument.Id, out Hub? hub))
      {
        hub.Stop();
      }

      HubRecords.DeleteMany(from filesystem in HubRecords.AsQueryable() where filesystem.OwnerUserId == change.FullDocument.Id select filesystem);
    }, cancellationToken);

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

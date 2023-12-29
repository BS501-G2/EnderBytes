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
  private readonly WaitQueue<(TaskCompletionSource<Hub.Session> source, ConnectionService.Connection connection, Record.StorageHub storageHub, KeyService.Transformer.Key inputKey)> WaitQueue = new(0);

  public async Task<Hub.Session> Get(Record.StorageHub storageHub, ConnectionService.Connection connection, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<Hub.Session> source = new();

    await WaitQueue.Enqueue((source, connection, storageHub, await connection.Session!.GetKeyTransformer(storageHub.KeySharedId)), cancellationToken);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, connection, record, inputKey) in WaitQueue.WithCancellation(cancellationToken))
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!Hubs.TryGetValue(record.Id, out Hub? hub))
      {
        hub = Hubs[record.Id] = Hub.StartHub(this, record, inputKey, cancellationToken);
      }

      source.SetResult(await hub.NewSession(connection, cancellationToken));
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

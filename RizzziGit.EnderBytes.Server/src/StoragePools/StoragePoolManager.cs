namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;
using Connections;
using Database;

public sealed class StoragePoolManager(Server server) : Service
{
  public readonly Server Server = server;

  public ResourceManager Resources => Server.Resources;
  public Database Database => Server.Resources.Database;

  private readonly WaitQueue<(TaskCompletionSource<StoragePool> source, KeyResource key, Connection.SessionInformation? session, StoragePoolResource pool)> WaitQueue = new(0);
  private readonly WeakDictionary<StoragePoolResource, StoragePool> Handles = [];

  public async Task<StoragePool> GetHandle(StoragePoolResource pool, Connection.SessionInformation? session, CancellationToken cancellationToken)
  {
    KeyResource? key = await Database.RunTransaction((transaction) => Resources.Keys.GetBySharedId(transaction, pool.KeySharedId, session?.Transformer.UserKey.SharedId), CancellationToken.None);
    TaskCompletionSource<StoragePool> source = new();

    await WaitQueue.Enqueue((source, key!, session, pool), cancellationToken);
    return await source.Task;
  }

  private StoragePool GetPool(KeyResource key, Connection.SessionInformation? session, StoragePoolResource pool)
  {
    if (!Handles.TryGetValue(pool, out var handle))
    {
      handle = pool.Type switch
      {
        StoragePoolType.Blob => new BlobStoragePool(this, pool, Convert.ToHexString(key.DecryptPrivateKey(session?.Transformer))),
        _ => throw new InvalidOperationException("Unknown type."),
      };

      handle.Start(CancellationToken.None);
      Handles.Add(pool, handle);
    }

    return handle;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, key, session, pool) in WaitQueue)
    {
      try
      {
        source.SetResult(GetPool(key, session, pool));
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;
using Connections;
using Database;

public sealed class StoragePoolManager : Service
{
  public StoragePoolManager(Server server) : base("Storage Pools")
  {
    Server = server;

    server.Resources.Server.Resources.StoragePools.ResourceDeleted += (transaction, resource) =>
    {
      if (Handles.TryGetValue(resource, out var storagePool))
      {
        storagePool.Stop();
      }
    };
  }

  public readonly Server Server;

  public ResourceManager Resources => Server.Resources;
  public Database Database => Server.Resources.Database;

  private readonly WaitQueue<(TaskCompletionSource<StoragePool> source, KeyResource key, Connection.SessionInformation? session, StoragePoolResource pool)> WaitQueue = new(0);
  private readonly WeakDictionary<StoragePoolResource, StoragePool> Handles = [];

  public async Task<StoragePool> Get(StoragePoolResource resource, Connection.SessionInformation? session, CancellationToken cancellationToken)
  {
    if (!resource.IsValid)
    {
      throw new InvalidOperationException();
    }

    KeyResource? key = await Database.RunTransaction((transaction) => Resources.Keys.GetBySharedId(transaction, resource.KeySharedId, session?.UserKeyTransformer.UserKey.SharedId), CancellationToken.None);
    if (!resource.IsValid)
    {
      throw new InvalidOperationException();
    }

    TaskCompletionSource<StoragePool> source = new();

    await WaitQueue.Enqueue((source, key!, session, resource), cancellationToken);
    return await source.Task;
  }

  private StoragePool GetPool(KeyResource key, Connection.SessionInformation? session, StoragePoolResource pool)
  {
    if (!(pool.IsValid && key.IsValid))
    {
      throw new InvalidOperationException();
    }

    if (!Handles.TryGetValue(pool, out var handle))
    {
      handle = pool.Type switch
      {
        StoragePoolType.Blob => new BlobStoragePool(this, pool, Convert.ToHexString(key.DecryptPrivateKey(session?.UserKeyTransformer))),

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

namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;

public sealed class StoragePoolManager : Service
{
  public StoragePoolManager(Server server) : base("Storage Pools")
  {
    Server = server;
    WaitQueue = new();
    StoragePools = new();

    Server.Resources.StoragePools.OnResourceDelete((_, resource) => {
      lock (this)
      {
        if (StoragePools.TryGetValue(resource, out var storagePool))
        {
          storagePool.Stop();
        }
      }
    });
  }

  public readonly Server Server;
  private readonly WeakDictionary<StoragePoolResource, StoragePool> StoragePools;
  private WaitQueue<(TaskCompletionSource<StoragePool> source, StoragePoolResource resource)> WaitQueue;

  public async Task<StoragePool> GetStoragePool(StoragePoolResource storagePool, CancellationToken cancellationToken)
  {
    TaskCompletionSource<StoragePool> source = new();
    await WaitQueue.Enqueue((source, storagePool), cancellationToken);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var (source, resource) = await WaitQueue.Dequeue(cancellationToken);

      lock (this)
      {
        {
          if (StoragePools.TryGetValue(resource, out var storagePool))
          {
            source.SetResult(storagePool);
            continue;
          }
        }

        {
          StoragePool? storagePool = resource.Type switch
          {
            StoragePoolType.Blob => new BlobStoragePool(this, resource),

            _ => null
          };

          if (storagePool == null)
          {
            source.SetException(new InvalidOperationException("Unknown type."));
            continue;
          }

          storagePool.Stopped += (_, _) =>
          {
            lock (this)
            {
              StoragePools.Remove(resource);
            }
          };

          StoragePools.Add(resource, storagePool);
          storagePool.Start(cancellationToken);
        }
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    try { WaitQueue.Dispose(); } catch { }
    WaitQueue = new();

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception)
  {
    try { WaitQueue.Dispose(); } catch { }
    return Task.CompletedTask;
  }
}

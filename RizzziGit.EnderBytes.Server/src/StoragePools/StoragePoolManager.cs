namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;

public sealed class StoragePoolManager : Service
{
  public StoragePoolManager(Server server) : base("Storage Pools", server)
  {
    Server = server;
    WaitQueue = new();
    StoragePools = new();

    Server.Resources.StoragePools.OnResourceDelete += (_, resource) =>
    {
      lock (this)
      {
        StoragePools.Remove(resource);
      }
    };
  }

  public readonly Server Server;
  private readonly WeakDictionary<StoragePoolResource, IStoragePool> StoragePools;
  private WaitQueue<(TaskCompletionSource<IStoragePool> source, StoragePoolResource resource)> WaitQueue;
  public async Task<IStoragePool> GetStoragePool(StoragePoolResource storagePool, CancellationToken cancellationToken)
  {
    TaskCompletionSource<IStoragePool> source = new();
    await WaitQueue.Enqueue((source, storagePool), cancellationToken);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    FileStream? blobFile = null;
    try
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
            IStoragePool? storagePool = null;
            switch (resource.Type)
            {
              case StoragePoolType.Blob:
                storagePool = new BlobStoragePool(this, resource, blobFile ??= File.Open(Server.Configuration.BlobPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
                break;
              case StoragePoolType.Physical:
                // storagePool = new PhysicalStoragePool(this, resource);
                // break;
              case StoragePoolType.Remote:
                // storagePool = new RemoteStoragePool(this, resource);
                // break;

              default:
                source.SetException(new System.InvalidOperationException("Unknown type."));
                break;
            }

            if (storagePool != null)
            {
              StoragePools.Add(resource, storagePool);
              source.SetResult(storagePool);
            }
          }
        }
      }
    }
    finally
    {
      blobFile?.Close();
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

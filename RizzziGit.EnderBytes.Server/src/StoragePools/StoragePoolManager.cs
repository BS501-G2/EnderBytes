namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;

public sealed class StoragePoolManager : Service
{
  private readonly WaitQueue<(TaskCompletionSource<StoragePool> source, KeyResource.Transformer transformer, StoragePoolResource pool)> WaitQueue = new(0);
  private readonly WeakDictionary<StoragePoolResource, StoragePool> Handles = [];

  public async Task<StoragePool> GetHandle(StoragePoolResource pool, KeyResource.Transformer transformer, CancellationToken cancellationToken)
  {
    if (transformer.Key.Id != pool.KeyId)
    {
      throw new InvalidOperationException("Invalid key transformer.");
    }

    TaskCompletionSource<StoragePool> source = new();

    await WaitQueue.Enqueue((source, transformer, pool), cancellationToken);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, transformer, pool) in WaitQueue)
    {
      if (!Handles.TryGetValue(pool, out var handle))
      {
        switch (pool.Type)
        {
          case StoragePoolType.Blob:
            handle = new BlobStoragePool(this,pool, transformer);
            break;

          default: continue;
        }

        handle.Start(CancellationToken.None);
        Handles.Add(pool, handle);
      }

      source.SetResult(handle);
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

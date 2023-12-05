namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Utilities;

public abstract partial class StoragePool(StoragePoolManager manager, StoragePoolResource resource, KeyResource.Transformer transformer) : Lifetime($"#{resource.Id}")
{
  public readonly StoragePoolManager Manager = manager;
  public readonly StoragePoolResource Resource = resource;
  protected readonly KeyResource.Transformer Transformer = transformer;

  public long LastActiveTime { get; private set; }
  public Task? UnderlyingTask { get; private set; }

  protected void ValidateTransformer(KeyResource.Transformer transformer)
  {
    if (transformer.Key != Transformer.Key)
    {
      throw new InvalidOperationException("Invalid key transformer.");
    }
  }

  protected abstract Task<Node.Folder> Internal_GetRootFolder(KeyResource.Transformer transformer);
  protected abstract Task<TrashItem[]> Internal_ListTrashItems(KeyResource.Transformer transformer);

  protected abstract Task Internal_OnStart();
  protected abstract Task Internal_OnStop();

  public Task<Node.Folder> GetRootFolder(KeyResource.Transformer transformer) => Internal_GetRootFolder(transformer);
  public Task<TrashItem[]> ListTrashItems(KeyResource.Transformer transformer) => Internal_ListTrashItems(transformer);

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    TaskCompletionSource source = new();

    lock (this)
    {
      if (UnderlyingTask != null)
      {
        throw new InvalidOperationException("Already running.");
      }

      UnderlyingTask = source.Task;
    }

    try
    {
      await Internal_OnStart();
      await base.OnRun(cancellationToken);
    }
    finally
    {
      try
      {
        await Internal_OnStop();
      }
      finally
      {
        lock (this)
        {
          source.SetResult();
          UnderlyingTask = null;
        }
      }
    }
  }

  public new Task Stop()
  {
    base.Stop();
    lock (this)
    {
      return UnderlyingTask ?? Task.CompletedTask;
    }
  }
}

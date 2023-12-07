namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Utilities;

public abstract partial class StoragePool(StoragePoolManager manager, StoragePoolResource resource) : Lifetime($"#{resource.Id}")
{
  public readonly StoragePoolManager Manager = manager;
  public readonly StoragePoolResource Resource = resource;

  public long LastActiveTime { get; private set; }
  public Task? UnderlyingTask { get; private set; }

  protected abstract Task<INode.IFolder> IGetRootFolder(KeyResource.Transformer transformer);
  protected abstract Task<TrashItem[]> IListTrashItems(KeyResource.Transformer transformer);

  protected abstract Task IOnStart();
  protected abstract Task IOnStop();

  public Task<INode.IFolder> GetRootFolder(KeyResource.Transformer transformer) => IGetRootFolder(transformer);
  public Task<TrashItem[]> ListTrashItems(KeyResource.Transformer transformer) => IListTrashItems(transformer);

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
      await IOnStart();
      await base.OnRun(cancellationToken);
    }
    finally
    {
      try
      {
        await IOnStop();
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

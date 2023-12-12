namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Utilities;
using Connections;

public abstract partial class StoragePool(StoragePoolManager manager, StoragePoolResource resource) : Lifetime($"#{resource.Id}")
{
  public readonly StoragePoolManager Manager = manager;
  public readonly StoragePoolResource Resource = resource;

  public long LastActiveTime { get; private set; }
  public Task? UnderlyingTask { get; private set; }

  protected abstract Task<INode.IFolder> IGetRootFolder(Connection connection);
  protected abstract Task<TrashItem[]> IListTrashItems(Connection connection);

  protected abstract Task IOnStart();
  protected abstract Task IOnStop();

  public Task<INode.IFolder> GetRootFolder(Connection connection) => IGetRootFolder(connection);
  public Task<TrashItem[]> ListTrashItems(Connection connection) => IListTrashItems(connection);

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

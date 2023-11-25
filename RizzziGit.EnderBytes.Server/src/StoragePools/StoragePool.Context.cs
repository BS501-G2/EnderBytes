namespace RizzziGit.EnderBytes.StoragePools;

using Connections;

public abstract partial class StoragePool
{
  public abstract class Context(StoragePool pool, Connection connection)
  {
    public readonly StoragePool Pool = pool;
    public readonly Connection Connection = connection;
  }

  protected abstract Task<Root> InternalGetRoot(Context context, CancellationToken cancellationToken);
  protected abstract IAsyncEnumerable<Handle> InternalGetTrashed(Context context, CancellationToken cancellationToken);
  protected abstract Task<Handle> InternalGetByPath(Context context, Path path, CancellationToken cancellationToken);

  public async Task<Root> GetRoot(Context context, CancellationToken cancellationToken)
  {
    return await InternalGetRoot(context, cancellationToken);
  }

  public async Task<Handle> GetByPath(Context context, Path path, CancellationToken cancellationToken)
  {
    return await InternalGetByPath(context, path, cancellationToken);
  }

  public async Task<Handle[]> GetTrashed(Context context, CancellationToken cancellationToken)
  {
    List<Handle> list = [];

    await foreach (Handle handle in InternalGetTrashed(context, cancellationToken))
    {
      if (handle.TrashTime == null)
      {
        throw new InvalidOperationException("Invalid trash time.");
      }

      list.Add(handle);
    }

    return [.. list];
  }
}

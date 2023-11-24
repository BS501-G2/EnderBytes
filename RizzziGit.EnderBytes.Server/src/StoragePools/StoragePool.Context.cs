namespace RizzziGit.EnderBytes.StoragePools;

using Connections;

public abstract partial class StoragePool
{
  public abstract class Context(StoragePool pool, Connection connection)
  {
    public readonly StoragePool Pool = pool;
    public readonly Connection Connection = connection;

    protected abstract Task<Handle.Folder> InternalGetRoot(CancellationToken cancellationToken);
    protected abstract IAsyncEnumerable<Handle> InternalGetTrashed(CancellationToken cancellationToken);
    protected abstract Task<Handle> InternalGetByPath(Path path, CancellationToken cancellationToken);

    public async Task<Handle.Folder> GetRoot(CancellationToken cancellationToken)
    {
      return await InternalGetRoot(cancellationToken);
    }

    public async Task<Handle> GetByPath(Path path, CancellationToken cancellationToken)
    {
      if (path.Length == 0)
      {
        return await GetRoot(cancellationToken);
      }

      return await InternalGetByPath(path, cancellationToken);
    }

    public async Task<Handle[]> GetTrashed(CancellationToken cancellationToken)
    {
      List<Handle> list = [];

      await foreach (Handle handle in InternalGetTrashed(cancellationToken))
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
}

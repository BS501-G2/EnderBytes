namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract class Root(StoragePool storagePool)
  {
    public readonly StoragePool StoragePool = storagePool;

    protected abstract Task<Handle.Folder> InternalGetRootFolder(Context context, CancellationToken cancellationToken);
    protected abstract IAsyncEnumerable<Handle> InternalListTrashed(Context context, CancellationToken cancellationToken);

    protected abstract Task<Handle> InternalGetByPath(Context context, Path path, CancellationToken cancellationToken);

    public async Task<Handle.Folder> GetRootFolder(Context context, CancellationToken cancellationToken)
    {
      return await GetRootFolder(context, cancellationToken);
    }

    public async Task<Handle> GetByPath(Context context, Path path, CancellationToken cancellationToken)
    {
      return await InternalGetByPath(context, path, cancellationToken);
    }

    public async Task<Handle[]> ListTrashed(Context context, CancellationToken cancellationToken)
    {
      List<Handle> handles = [];

      await foreach (Handle handle in InternalListTrashed(context, cancellationToken))
      {
        handles.Add(handle);
      }

      return [.. handles];
    }
  }
}

namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class Folder : Handle
    {
      protected Folder(StoragePool pool, Folder? parent) : base(pool, parent) { }

      protected abstract IAsyncEnumerable<Handle> InternalScan(CancellationToken cancellationToken);

      public async Task<Handle[]> Scan(CancellationToken cancellationToken)
      {
        List<Handle> handles = [];

        await foreach (Handle handle in InternalScan(cancellationToken))
        {
          handles.Add(handle);
        }

        return [.. handles];
      }
    }
  }
}

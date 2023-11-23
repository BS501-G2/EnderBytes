namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class Root : Folder
    {
      protected Root(StoragePool pool) : base(pool, null) { }

      public override Path Path => new(Pool);

      protected abstract Task<Handle> InternalGetHandle(Path path, CancellationToken cancellationToken);

      public async Task<Handle> GetHandle(Path path, CancellationToken cancellationToken)
      {
        return await InternalGetHandle(path, cancellationToken);
      }
    }
  }
}

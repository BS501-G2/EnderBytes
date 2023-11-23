namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class Root : Folder
    {
      protected Root(StoragePool pool) : base(pool) { }

      public override Path Path => new(Pool);

      protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken) => throw new NotSupportedException();

      protected abstract Task<Handle> InternalGetHandle(Path path, CancellationToken cancellationToken);

      public async Task<Handle> GetHandle(Path path, CancellationToken cancellationToken)
      {
        return await InternalGetHandle(path, cancellationToken);
      }
    }
  }
}

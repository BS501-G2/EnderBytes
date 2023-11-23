namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class SymbolicLink : Handle
    {
      protected SymbolicLink(StoragePool pool, Folder? parent) : base(pool, parent) { }

      public abstract Task<Path> GetTargetPath(CancellationToken cancellationToken);

      public async Task<Handle> GetTargetHandle(CancellationToken cancellationToken) => await (await Pool.GetRoot(cancellationToken)).GetHandle(await GetTargetPath(cancellationToken), cancellationToken);
    }
  }
}

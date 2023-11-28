namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class SymbolicLink : Handle
    {
      protected SymbolicLink(StoragePool pool) : base(pool) { }

      public abstract Task<Path> GetTargetPath(Context context, CancellationToken cancellationToken);

      public async Task<Handle> GetTargetHandle(Context context, CancellationToken cancellationToken) => await (await Pool.GetRoot(context, cancellationToken)).GetByPath(context, await GetTargetPath(context, cancellationToken), cancellationToken);
    }
  }
}

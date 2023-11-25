namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  private sealed class BlobSymbolicLinkHandle(StoragePool pool) : Handle.SymbolicLink(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    public override Task<Path> GetTargetPath(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}

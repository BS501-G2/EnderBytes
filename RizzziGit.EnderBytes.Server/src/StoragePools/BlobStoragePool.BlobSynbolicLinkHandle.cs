namespace RizzziGit.EnderBytes.StoragePools;

using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobSymbolicLinkHandle(StoragePool pool, FileNodeResource resource) : Handle.SymbolicLink(pool)
  {
    private readonly FileNodeResource Resource = resource;

    public override long Id => Resource.Id;

    public override Task<long?> GetAccessTime(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<string> GetName(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

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

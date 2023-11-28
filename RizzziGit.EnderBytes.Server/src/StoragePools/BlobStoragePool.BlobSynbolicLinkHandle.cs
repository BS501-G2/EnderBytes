namespace RizzziGit.EnderBytes.StoragePools;

using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobSymbolicLinkHandle(StoragePool pool, FileNodeResource resource) : Handle.SymbolicLink(pool)
  {
    public readonly FileNodeResource Resource = resource;

    public override long Id => Resource.Id;

    public override Task<Path> GetTargetPath(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<long?> InternalGetAccessTime(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<string> InternalGetName(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<long?> InternalGetTrashTime(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalTrash(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}

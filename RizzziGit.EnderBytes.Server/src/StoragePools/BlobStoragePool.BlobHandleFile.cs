namespace RizzziGit.EnderBytes.StoragePools;

using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFileHandle(StoragePool pool, FileNodeResource resource) : Handle.File(pool)
  {
    private readonly FileNodeResource Resource = resource;

    public override long Id => Resource.Id;

    public override Task<Snapshot> CreateSnapshot(Context context, Snapshot? baseSnapshot, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<long?> GetAccessTime(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<string> GetName(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override IAsyncEnumerable<Snapshot> GetSnapshots(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Stream> InternalOpen(Context context, Snapshot snapshot, Access access, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}

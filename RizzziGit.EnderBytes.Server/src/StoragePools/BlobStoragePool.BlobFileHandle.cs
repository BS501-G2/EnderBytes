namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFileHandle(StoragePool pool) : Handle.File(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    public override Task<Snapshot> CreateSnapshot(Context context, Snapshot? baseSnapshot, CancellationToken cancellationToken)
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

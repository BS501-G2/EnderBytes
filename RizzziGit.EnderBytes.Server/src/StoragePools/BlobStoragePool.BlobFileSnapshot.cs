namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFileSnapshot(Handle.File file) : Handle.File.Snapshot(file)
  {
    public override long Id => throw new NotImplementedException();
    public override long? CreateTime => throw new NotImplementedException();
    public override long? UpdateTime => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();

    public override Task<Handle.File.Snapshot?> GetParentSnapshot()
    {
      throw new NotImplementedException();
    }
  }
}

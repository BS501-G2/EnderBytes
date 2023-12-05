namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  public sealed class SymbolicLinkNode : Node.SymbolicLink
  {
    public override Path? Target => throw new NotImplementedException();
    public override StoragePool Handle => throw new NotImplementedException();
    public override long CreateTime => throw new NotImplementedException();
    public override long AccessTime => throw new NotImplementedException();
    public override long ModifyTime => throw new NotImplementedException();
    public override long UserId => throw new NotImplementedException();
    public override long KeyId => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Folder? Parent => throw new NotImplementedException();
  }
}

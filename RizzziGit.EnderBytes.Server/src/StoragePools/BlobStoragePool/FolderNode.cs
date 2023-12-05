using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  public sealed class FolderNode : Node.Folder
  {
    public override StoragePool Handle => throw new NotImplementedException();
    public override long CreateTime => throw new NotImplementedException();
    public override long AccessTime => throw new NotImplementedException();
    public override long ModifyTime => throw new NotImplementedException();
    public override long UserId => throw new NotImplementedException();
    public override long KeyId => throw new NotImplementedException();
    public override string Name => throw new NotImplementedException();
    public override Folder? Parent => throw new NotImplementedException();

    protected override Task<Node[]> Internal_Scan(KeyResource.Transformer transformer)
    {
      throw new NotImplementedException();
    }
  }
}

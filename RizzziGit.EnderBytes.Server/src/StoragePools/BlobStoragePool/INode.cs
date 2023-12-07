namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Buffer;

public sealed partial class BlobStoragePool
{
  private interface IBlobClass
  {
    public Server Server { get; }
    public Database Database { get; }
    public Resources.BlobStorage.ResourceManager ResourceManager { get; }
  }

  private interface IBlobFile : IBlobNode, INode.IFile, IBlobClass
  {
  }

  private interface IBlobStream : INode.IFile.IStream, IBlobClass
  {
  }

  private interface IBlobSnapshot : INode.IFile.ISnapshot, IBlobClass
  {
    public BlobSnapshotResource Resource { get; }
  }

  private interface IBlobFolder : IBlobNode, INode.IFolder, IBlobClass
  {
  }

  private interface IBlobSymbolicLink : IBlobNode, INode.ISymbolicLink, IBlobClass
  {
  }

  private interface IBlobNode : INode, IBlobClass
  {
    public new BlobStoragePool Pool { get; }
    public BlobNodeResource Resource { get; }

    StoragePool INode.Pool => Pool;
  }
}

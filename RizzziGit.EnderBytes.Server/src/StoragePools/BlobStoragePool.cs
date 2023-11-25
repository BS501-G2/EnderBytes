namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;

public sealed partial class BlobStoragePool : StoragePool
{
  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
  }

  private readonly Resources.BlobStorage.ResourceManager Resources;
  private Database Database => Resources.Database;

  private FileNodeResource.ResourceManager Nodes => Resources.Nodes;

  protected override Task InternalStart(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRun(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalStop(Exception? exception)
  {
    throw new NotImplementedException();
  }
}

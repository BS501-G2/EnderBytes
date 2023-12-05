namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Database;

public sealed partial class BlobStoragePool : StoragePool
{
  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource, KeyResource.Transformer transformer) : base(manager, resource, transformer)
  {
    // ResourceManager = new(this, transformer.);
  }

  private readonly Resources.BlobStorage.ResourceManager ResourceManager;
  private Database Database => ResourceManager.Database;

  protected override Task<Node.Folder> Internal_GetRootFolder(KeyResource.Transformer transformer)
  {
    ValidateTransformer(transformer);
    throw new NotImplementedException();
  }

  protected override Task<TrashItem[]> Internal_ListTrashItems(KeyResource.Transformer transformer)
  {
    ValidateTransformer(transformer);
    throw new NotImplementedException();
  }

  protected override Task Internal_OnStart()
  {
    throw new NotImplementedException();
  }

  protected override Task Internal_OnStop()
  {
    throw new NotImplementedException();
  }
}

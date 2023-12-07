namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Collections;

public sealed partial class BlobStoragePool : StoragePool
{
  private const string ROOT_FOLDER = ".ROOT";
  private const string TRASH_FOLDER = ".TRASH_FOLDER";

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource, string password) : base(manager, resource)
  {
    ResourceManager = new(this, password);
    Nodes = [];
  }

  private readonly Resources.BlobStorage.ResourceManager ResourceManager;
  private readonly WeakDictionary<BlobNodeResource, INode> Nodes;
  private IBlobFolder? RootFolder;

  private Database Database => ResourceManager.Database;

  private INode ResolveNode(BlobNodeResource resource)
  {
    if (!Nodes.TryGetValue(resource, out var node))
    {
      Nodes.Add(resource, node = resource.Type switch
      {
        BlobNodeType.Folder => new Node.Folder(this, resource),
        BlobNodeType.File => new Node.File(this, resource),
        BlobNodeType.SymbolicLink => new Node.SymbolicLink(this, resource),

        _ => throw new NotImplementedException(),
      });
    }

    return node;
  }

  protected override async Task<INode.IFolder> IGetRootFolder(KeyResource.Transformer transformer) =>
    RootFolder ??= (IBlobFolder)ResolveNode(await RunTask((cancellationToken) =>
      Database.RunTransaction((transaction) =>
        ResourceManager.Nodes.GetByName(transaction, ROOT_FOLDER, null)
          ?? ResourceManager.Nodes.Create(transaction, ROOT_FOLDER, BlobNodeType.Folder, null, transformer)
      ), CancellationToken.None));

  protected override Task<TrashItem[]> IListTrashItems(KeyResource.Transformer transformer)
  {
    throw new NotImplementedException();
  }

  protected override Task IOnStart()
  {
    throw new NotImplementedException();
  }

  protected override Task IOnStop()
  {
    throw new NotImplementedException();
  }
}

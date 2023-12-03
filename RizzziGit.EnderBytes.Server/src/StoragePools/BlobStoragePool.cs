namespace RizzziGit.EnderBytes.StoragePools;

using Database;
using Resources;
using Resources.BlobStorage;
using Collections;

public sealed class BlobStoragePool : StoragePool
{
  private interface IBlobNode
  {
    public FileNodeResource Resource { get; }
    public BlobFolderNode? BlobParent { get; }
  }

  private sealed class BlobFolderNode(StoragePool pool, BlobFolderNode? parent, FileNodeResource resource) : FolderNode(pool), IBlobNode
  {
    public readonly FileNodeResource Resource = resource;
    public BlobFolderNode? BlobParent = parent;

    FileNodeResource IBlobNode.Resource => Resource;
    BlobFolderNode? IBlobNode.BlobParent => BlobParent;

    public override long Id => Resource.Id;
    public override FolderNode? Parent => BlobParent;
    public override string Name => Resource.Name;
    }

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
    Nodes = [];
  }

  private readonly Resources.BlobStorage.ResourceManager Resources;
  private readonly DoublyWeakDictionary<FileNodeResource, Node> Nodes;

  private Node ResolveNode(FileNodeResource resource, BlobFolderNode? parent)
  {
    if (!Nodes.TryGetValue(resource, out var node))
    {
      Nodes.Add(resource, node = resource.Type switch
      {
        FileNodeType.Directory => new BlobFolderNode(this, parent, resource),

        _ => throw new NotImplementedException()
      });
    }
    else
    {
      ((BlobFolderNode)node).BlobParent = (BlobFolderNode)ResolveNode(resource, parent?.BlobParent);
    }

    return node;
  }

  public Task RunTransaction(Database.TransactionHandler handler, CancellationToken cancellationToken) => Resources.Database.RunTransaction(handler, cancellationToken);
  public Task<T> RunTransaction<T>(Database.TransactionHandler<T> handler, CancellationToken cancellationToken) => Resources.Database.RunTransaction(handler, cancellationToken);

  protected override Task Internal_OnRun(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task Internal_OnStart(CancellationToken cancellationToken) => Resources.Start();
  protected override Task Internal_OnStop(System.Exception? exception) => Resources.Stop();

  protected override Task<FileNode> Internal_CreateFile(Context context, FolderNode parent, string name, long preallocateSize) => RunTransaction<FileNode>((transaction) =>
  {
    throw new NotImplementedException();
  }, CancellationToken.None);

  protected override Task<FolderNode> Internal_CreateFolder(Context context, FolderNode parent, string name) => RunTransaction<FolderNode>((transaction) =>
  {
    FileNodeResource resource = Resources.Nodes.CreateFolder(transaction, name, ((BlobFolderNode)parent).Resource);

    return (BlobFolderNode)ResolveNode(resource, (BlobFolderNode)parent);
  }, CancellationToken.None);

  protected override Task<SymbolicLinkNode> Internal_CreateSymbolicLink(Context context, FolderNode parent, string name, Path target) => RunTransaction<SymbolicLinkNode>((transaction) =>
  {
    throw new NotImplementedException();
  }, CancellationToken.None);

  protected override Task<Node[]> Internal_Scan(Context context, FolderNode folder) => RunTransaction<Node[]>((transaction) =>
  {
    List<Node> nodes = [];
    foreach (FileNodeResource resource in Resources.Nodes.StreamChildrenNodes(transaction, ((BlobFolderNode)folder).Resource))
    {
      switch (resource.Type)
      {
        case FileNodeType.Directory:
          nodes.Add(ResolveNode(resource, ((IBlobNode)folder).BlobParent));
          break;
      }
    }

    return [.. nodes];
  }, CancellationToken.None);

  protected override Task Internal_SetName(Context context, Node node, string name) => RunTransaction((transaction) =>
  {
    Resources.Nodes.UpdateName(transaction, ((IBlobNode)node).Resource, name);
  }, CancellationToken.None);

  protected override Task Internal_SetParent(Context context, Node node, FolderNode? parent) => RunTransaction((transaction) =>
  {
    Resources.Nodes.UpdateParentFolder(transaction, ((IBlobNode)node).Resource, ((IBlobNode)node).Resource);
  }, CancellationToken.None);
}

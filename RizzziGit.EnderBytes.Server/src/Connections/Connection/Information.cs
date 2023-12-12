namespace RizzziGit.EnderBytes.Connections;

using Resources;
using StoragePools;

public abstract partial class Connection
{
  public abstract record NodeInformation(
    StoragePool Pool,
    KeyResource.Transformer PoolTransformer,
    KeyResource.Transformer NodeTransformer,
    UserResource Owner,
    StoragePool.INode Node
  )
  {
    public sealed record File(
      StoragePool Pool,
      KeyResource.Transformer PoolTransformer,
      KeyResource.Transformer NodeTransformer,
      UserResource Owner,
      StoragePool.INode.IFile FileNode
    ) : NodeInformation(Pool, PoolTransformer, NodeTransformer, Owner, FileNode);

    public sealed record Folder(
      StoragePool Pool,
      KeyResource.Transformer PoolTransformer,
      KeyResource.Transformer NodeTransformer,
      UserResource Owner,
      StoragePool.INode.IFolder FolderNode
    ) : NodeInformation(Pool, PoolTransformer, NodeTransformer, Owner, FolderNode);

    public sealed record SymbolicLink(
      StoragePool Pool,
      KeyResource.Transformer PoolTransformer,
      KeyResource.Transformer NodeTransformer,
      UserResource Owner,
      StoragePool.INode.ISymbolicLink SymbolicLinkNode
    ) : NodeInformation(Pool, PoolTransformer, NodeTransformer, Owner, SymbolicLinkNode);
  }

  public abstract record Information(
    NodeInformation? NodeReference,
    string Name,
    long CreateTime,
    long UpdateTime,
    long AccessTime
  )
  {
    public sealed record File(
      NodeInformation.File FileReference,
      string Name,
      long CreateTime,
      long UpdateTime,
      long AccessTime,
      long Size
    ) : Information(FileReference, Name, CreateTime, UpdateTime, AccessTime);

    public sealed record Folder(
      NodeInformation.Folder? Reference,
      string Name,
      long CreateTime,
      long UpdateTime,
      long AccessTime
    ) : Information(Reference, Name, CreateTime, UpdateTime, AccessTime);

    public sealed record SymbolicLink(
      NodeInformation.SymbolicLink Reference,
      string Name,
      long CreateTime,
      long UpdateTime,
      long AccessTime
    ) : Information(Reference, Name, CreateTime, UpdateTime, AccessTime);
  }
}

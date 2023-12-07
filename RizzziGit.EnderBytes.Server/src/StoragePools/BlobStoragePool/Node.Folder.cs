namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Connections;

public sealed partial class BlobStoragePool
{
  public abstract partial class Node
  {
    public sealed class Folder : Node, IBlobFolder
    {
      public Folder(BlobStoragePool pool, BlobNodeResource resource) : base(pool, resource)
      {
      }

      async Task<INode.IFile> INode.IFolder.ICreateFile(Connection connection, string name, long preallocateSize)
      {
        var (privateKey, publicKey) = Server.KeyGenerator.GetNew();
        return await Database.RunTransaction((transaction) =>
        {
          KeyResource key = ResourceManager.Keys.Create(transaction, connection.Session?.Transformer, privateKey, publicKey);
          return (IBlobFile)Pool.ResolveNode(ResourceManager.Nodes.Create(transaction, name, BlobNodeType.File, Resource, key.GetTransformer()));
        });
      }

      async Task<INode.IFolder> INode.IFolder.ICreateFolder(Connection connection, string name)
      {
        var (privateKey, publicKey) = Server.KeyGenerator.GetNew();
        return await Database.RunTransaction((transaction) =>
        {
          KeyResource key = ResourceManager.Keys.Create(transaction, connection.Session?.Transformer, privateKey, publicKey);
          return (IBlobFolder)Pool.ResolveNode(ResourceManager.Nodes.Create(transaction, name, BlobNodeType.Folder, Resource, key.GetTransformer()));
        });
      }

      async Task<INode.ISymbolicLink> INode.IFolder.ICreateSymbolicLink(Connection connection, string name, Path target)
      {
        var (privateKey, publicKey) = Server.KeyGenerator.GetNew();
        return await Database.RunTransaction((transaction) =>
        {
          KeyResource key = ResourceManager.Keys.Create(transaction, connection.Session?.Transformer, privateKey, publicKey);
          return (IBlobSymbolicLink)Pool.ResolveNode(ResourceManager.Nodes.Create(transaction, name, BlobNodeType.SymbolicLink, Resource, key.GetTransformer()));
        });
      }

      async Task<INode[]> INode.IFolder.IScan(Connection connection)
      {
        List<INode> nodes = [];

        await Database.RunTransaction((transaction) =>
        {
          foreach (BlobNodeResource resource in ResourceManager.Nodes.StreamChildren(transaction, Resource))
          {
            nodes.Add(Pool.ResolveNode(resource));
          }
        });

        return [.. nodes];
      }
    }
  }
}

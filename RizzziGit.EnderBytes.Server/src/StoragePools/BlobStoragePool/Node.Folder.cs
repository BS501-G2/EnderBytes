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

      public Task<INode.IFile> ICreateFile(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, long preallocateSize)
      {
        throw new NotImplementedException();
      }

      public Task<INode.ISymbolicLink> ICreateSymbolicLink(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, Path target)
      {
        throw new NotImplementedException();
      }

      Task<INode.IFile> INode.IFolder.ICreateFile(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, long preallocateSize)
      {
        throw new NotImplementedException();
      }

      Task<INode.IFolder> INode.IFolder.ICreateFolder(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name)
      {
        throw new NotImplementedException();
      }

      Task<INode.ISymbolicLink> INode.IFolder.ICreateSymbolicLink(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, Path target)
      {
        throw new NotImplementedException();
      }

      Task<INode?> INode.IFolder.IGetByPath(KeyResource.Transformer transformer, Path path)
      {
        throw new NotImplementedException();
      }

      Task<INode[]> INode.IFolder.IScan(KeyResource.Transformer transformer)
      {
        throw new NotImplementedException();
      }
    }
  }
}

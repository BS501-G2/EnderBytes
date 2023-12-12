namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Connections;

public abstract partial class StoragePool
{
  public partial interface INode
  {
    public interface IFolder : INode
    {
      Task<INode[]> IScan(KeyResource.Transformer transformer);
      Task<INode?> IGetByPath(KeyResource.Transformer transformer, Path path);
      Task<IFile> ICreateFile(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, long preallocateSize);
      Task<IFolder> ICreateFolder(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name);
      Task<ISymbolicLink> ICreateSymbolicLink(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, Path target);

      public Task<INode[]> Scan(KeyResource.Transformer transformer) => IScan(transformer);
      public Task<INode?> GetByPath(KeyResource.Transformer transformer, Path path) => IGetByPath(transformer, path);
      public Task<IFile> CreateFile(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, long preallocateSize) => ICreateFile(poolTransformer, nodeTransformer, name, preallocateSize);
      public Task<IFolder> CreateFolder(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name) => ICreateFolder(poolTransformer, nodeTransformer, name);
      public Task<ISymbolicLink> CreateSymbolicLink(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, string name, Path target) => ICreateSymbolicLink(poolTransformer, nodeTransformer, name, target);
    }
  }
}

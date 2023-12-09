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
      Task<IFile> ICreateFile(KeyResource.Transformer transformer, string name, long preallocateSize);
      Task<IFolder> ICreateFolder(KeyResource.Transformer transformer, string name);
      Task<ISymbolicLink> ICreateSymbolicLink(KeyResource.Transformer transformer, string name, Path target);

      public Task<INode[]> Scan(KeyResource.Transformer transformer) => IScan(transformer);
      public Task<INode?> GetByPath(KeyResource.Transformer transformer, Path path) => IGetByPath(transformer, path);
      public Task<IFile> CreateFile(KeyResource.Transformer transformer, string name, long preallocateSize) => ICreateFile(transformer, name, preallocateSize);
      public Task<IFolder> CreateFolder(KeyResource.Transformer transformer, string name) => ICreateFolder(transformer, name);
      public Task<ISymbolicLink> CreateSymbolicLink(KeyResource.Transformer transformer, string name, Path target) => ICreateSymbolicLink(transformer, name, target);
    }
  }
}

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Connections;

public abstract partial class StoragePool
{
  public partial interface INode
  {
    public interface IFolder : INode
    {
      Task<INode[]> IScan(Connection connection);
      Task<IFile> ICreateFile(Connection connection, string name, long preallocateSize);
      Task<IFolder> ICreateFolder(Connection connection, string name);
      Task<ISymbolicLink> ICreateSymbolicLink(Connection connection, string name, Path target);

      public Task<INode[]> Scan(Connection connection) => IScan(connection);
      public Task<IFile> CreateFile(Connection connection, string name, long preallocateSize) => ICreateFile(connection, name, preallocateSize);
      public Task<IFolder> CreateFolder(Connection connection, string name) => ICreateFolder(connection, name);
      public Task<ISymbolicLink> CreateSymbolicLink(Connection connection, string name, Path target) => ICreateSymbolicLink(connection, name, target);
    }
  }
}

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Buffer;
using Connections;

public sealed partial class BlobStoragePool
{
  public abstract partial class Node
  {
    public sealed partial class File : Node, IBlobFile
    {
      public sealed class Snapshot : IBlobSnapshot
      {
        public Snapshot(INode.IFile file, BlobSnapshotResource resource)
        {
          File = file;
          Resource = resource;
          Database = ((IBlobFile)file).Database;
          ResourceManager = ((IBlobFile)file).ResourceManager;
        }

        public readonly INode.IFile File;
        public readonly BlobSnapshotResource Resource;
        public readonly Database Database;
        public readonly Resources.BlobStorage.ResourceManager ResourceManager;

        INode.IFile INode.IFile.ISnapshot.File => File;
        BlobSnapshotResource IBlobSnapshot.Resource => Resource;
        Database IBlobClass.Database => Database;
        Resources.BlobStorage.ResourceManager IBlobClass.ResourceManager => ResourceManager;
        Server IBlobClass.Server => ResourceManager.Server;
      }

      public sealed class Stream : IBlobStream
      {
        public Stream(INode.IFile file, INode.IFile.ISnapshot snapshot)
        {
          File = file;
          Snapshot = snapshot;
        }

        public readonly INode.IFile File;
        public readonly INode.IFile.ISnapshot Snapshot;
        public Database Database => ((IBlobFile)File).Database;
        public Resources.BlobStorage.ResourceManager ResourceManager => ((IBlobFile)File).ResourceManager;
        public Server Server => ResourceManager.Server;

        INode.IFile INode.IFile.IStream.File => File;
        INode.IFile.ISnapshot INode.IFile.IStream.Snapshot => Snapshot;
        Database IBlobClass.Database => Database;
        Resources.BlobStorage.ResourceManager IBlobClass.ResourceManager => ResourceManager;
        Server IBlobClass.Server => Server;

        Task INode.IFile.IStream.ISeek(Connection connection, long position)
        {
          throw new NotImplementedException();
        }

        Task INode.IFile.IStream.ISetSize(Connection connection, long size)
        {
          throw new NotImplementedException();
        }

        Task<Buffer> INode.IFile.IStream.IRead(Connection connection, long count)
        {
          throw new NotImplementedException();
        }

        Task INode.IFile.IStream.IWrite(Connection connection, Buffer buffer)
        {
          throw new NotImplementedException();
        }
      }

      public File(BlobStoragePool pool, BlobNodeResource resource) : base(pool, resource)
      {
      }

      Task<INode.IFile.IStream> INode.IFile.IOpen(Connection connection, INode.IFile.Access access, INode.IFile.Mode mode)
      {
        throw new NotImplementedException();
      }
    }
  }
}

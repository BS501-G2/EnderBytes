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

        long INode.IFile.ISnapshot.Id => Resource.Id;
        long? INode.IFile.ISnapshot.BaseSnapshotId => Resource.BaseSnapshotId;
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

        Task INode.IFile.IStream.ISeek(KeyResource.Transformer transformer, long position)
        {
          throw new NotImplementedException();
        }

        Task INode.IFile.IStream.ISetSize(KeyResource.Transformer transformer, long size)
        {
          throw new NotImplementedException();
        }

        Task<Buffer> INode.IFile.IStream.IRead(KeyResource.Transformer transformer, long count)
        {
          throw new NotImplementedException();
        }

        Task INode.IFile.IStream.IWrite(KeyResource.Transformer transformer, Buffer buffer)
        {
          throw new NotImplementedException();
        }

        Task INode.IFile.IStream.IClose(KeyResource.Transformer poolTransformer)
        {
          throw new NotImplementedException();
        }
      }

      public File(BlobStoragePool pool, BlobNodeResource resource) : base(pool, resource)
      {
      }

      Task<INode.IFile.ISnapshot[]> INode.IFile.ISnapshotList(KeyResource.Transformer transformer)
      {
        throw new NotImplementedException();
      }

      Task<INode.IFile.IStream> INode.IFile.IOpen(KeyResource.Transformer poolTransformer, KeyResource.Transformer nodeTransformer, INode.IFile.ISnapshot snapshot, INode.IFile.Access access)
      {
        throw new NotImplementedException();
      }
    }
  }
}

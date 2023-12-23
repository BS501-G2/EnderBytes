using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Framework.Memory;

public enum BlobNodeType { File, Folder, SymbolicLink }

public sealed partial class StorageHubService
{
  public const int BUFFER_SIZE = KeyGeneratorService.KEY_SIZE / 8;

  public abstract partial class Hub
  {
    public sealed class Blob(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Hub(service, hubId, hubKey)
    {
      public new sealed class FileHandle(Hub hub, long fileId, long snapshotId, KeyGeneratorService.Transformer.Key transformer, HubFileAccess access) : Hub.FileHandle(hub, fileId, snapshotId, transformer, access)
      {
        private long CurrentIndex = 0;
        private long CurrentOffset = 0;

        protected override long Internal_Position => Position;
        protected override long Internal_Size => Size;

        protected override Task<CompositeBuffer> Internal_Read(long size)
        {
          throw new NotImplementedException();
        }

        protected override Task Internal_Seek(long position)
        {
          ArgumentOutOfRangeException.ThrowIfGreaterThan(Size, position);
          return Task.CompletedTask;
        }

        protected override Task Internal_SetSize(long size)
        {
          // Position = long.Min(Position, Size = size);
          return Task.CompletedTask;
        }

        protected override Task Internal_Write(CompositeBuffer buffer)
        {
          throw new NotImplementedException();
        }
      }

      private Server Server => Service.Server;
      private IMongoDatabase Database => Service.Server.Database;
      private MongoClient MongoClient => Service.Server.MongoClient;

      private IMongoCollection<Record.BlobStorageNode> Nodes => Server.GetCollection<Record.BlobStorageNode>();
      private IMongoCollection<Record.BlobStorageFileSnapshot> FileSnapshots => Server.GetCollection<Record.BlobStorageFileSnapshot>();
      private IMongoCollection<Record.BlobStorageFileDataMapper> FileDataMappers => Server.GetCollection<Record.BlobStorageFileDataMapper>();
      private IMongoCollection<Record.BlobStorageFileData> FileData => Server.GetCollection<Record.BlobStorageFileData>();

      protected override Task<Node.Folder> Internal_GetRootFolder()
      {
        throw new NotImplementedException();
      }

      protected override Task<TrashItem[]> Internal_ScanTrash()
      {
        throw new NotImplementedException();
      }
    }
  }
}

using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    public sealed partial class Blob : Hub
    {

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

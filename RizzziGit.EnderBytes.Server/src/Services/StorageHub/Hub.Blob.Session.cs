using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    private sealed partial class Blob
    {
      private sealed new class Session(Blob hub, ConnectionService.Connection connection) : Hub.Session(hub, connection)
      {
        public new readonly Blob Hub = hub;

        private IMongoClient MongoClient => Server.MongoClient;
        private IMongoDatabase MainDatabase => Server.MainDatabase;
        private IMongoDatabase BlobDatabase => Server.MongoClient.GetDatabase("EnderBytes.Blob");

        private IMongoCollection<Record.Key> Keys => MainDatabase.GetCollection<Record.Key>();
        private IMongoCollection<Record.BlobStorageNode> Nodes => BlobDatabase.GetCollection<Record.BlobStorageNode>();
        private IMongoCollection<Record.BlobStorageFileSnapshot> FileNodeSnapshots => BlobDatabase.GetCollection<Record.BlobStorageFileSnapshot>();
        private IMongoCollection<Record.BlobStorageFileDataMapper> FileDataMappers => BlobDatabase.GetCollection<Record.BlobStorageFileDataMapper>();
        private IMongoCollection<Record.BlobStorageFileData> FileData => BlobDatabase.GetCollection<Record.BlobStorageFileData>();

        private KeyService KeyService => Server.KeyService;

        private async Task<Record.BlobStorageNode> GetNode(long nodeId, CancellationToken cancellationToken) => await GetNode((long?)nodeId, cancellationToken) ?? throw new ArgumentException($"Invalid node id");
        private async Task<Record.BlobStorageNode?> GetNode(long? nodeId, CancellationToken cancellationToken) => nodeId != null
          ? await Nodes.FindOneAsync((node) => node.Id == nodeId, cancellationToken: cancellationToken) ?? throw new ArgumentException($"Invalid node id", nameof(nodeId))
          : null;

        // private async Task<Record.Key> GetKeyRecord(long keySharedId, CancellationToken cancellationToken)
        // {
        //   long? userId = Connection.Session?.UserId;
        //   return await Keys.FindOneAsync((key) => key.UserId == userId, cancellationToken: cancellationToken) ?? throw new ArgumentException($"No access.", nameof(keySharedId));
        // }

        private async Task<KeyService.Transformer.Key> GetKeyTransformer(Record.BlobStorageNode? node) => node != null ? await GetKeyTransformer(node!.KeySharedId) : Hub.HubKey;

        protected override Task<Node> Internal_FolderNodeCreate(long? parentFolderNodeId, string name) => Hub.RunTask((cancellationToken) => MongoClient.RunTransaction(async (cancellationToken) =>
        {
          Record.BlobStorageNode? parentNode = await GetNode(parentFolderNodeId, cancellationToken);
          KeyService.Transformer.Key parentKeyTransformer = await GetKeyTransformer(parentNode);

          if (Nodes.FindOne((node) => (parentNode == null || parentNode.Id == parentNode.Id) && node.Name == name, cancellationToken: cancellationToken) != null)
          {
            throw new ArgumentException($"Name already taken.", nameof(name));
          }

          return await MongoClient.RunTransaction(async (cancellationToken) =>
          {
            (long id, long createTime, long updateTime) = Record.GenerateNewId(Nodes);
            Record.Key key = await KeyService.CreateNewKey(Connection.Session!.Transformer);
            Record.BlobStorageNode node = new(id, createTime, updateTime, HubId, NodeType.Folder, parentNode?.Id, name, key.SharedId);
            Nodes.InsertOne(node, cancellationToken: cancellationToken);
            return new Node(node.Id, key.SharedId, node.CreateTime, node.UpdateTime, node.Name);
          }, cancellationToken: cancellationToken);
        }, cancellationToken: cancellationToken));

        protected override Task<Node[]> Internal_FolderNodeScan(long? folderNodeId) => Hub.RunTask((cancellationToken) => MongoClient.RunTransaction<Node[]>(async (cancellationToken) =>
        {
          Record.BlobStorageNode? folderNode = await GetNode(folderNodeId, cancellationToken);
          KeyService.Transformer.Key parentKeyTransformer = await GetKeyTransformer(folderNode);

          List<Node> nodes = [];
          {
            long? folderNodeId = folderNode?.Id;

            await MongoClient.RunTransaction((cancellationToken) =>
            {
              foreach (Record.BlobStorageNode node in Nodes.Find((record) => record.ParentNode == folderNodeId).ToEnumerable(cancellationToken: cancellationToken))
              {
                nodes.Add(new(node.Id, node.KeySharedId, node.CreateTime, node.UpdateTime, node.Name));
              }

              return Task.CompletedTask;
            }, cancellationToken: cancellationToken);
          }

          return [.. nodes];
        }, cancellationToken: cancellationToken));

        protected override Task Internal_NodeDelete(long fileNodeId) => Hub.RunTask((cancellationToken) => MongoClient.RunTransaction(async (cancellationToken) =>
        {
          Record.BlobStorageNode node = await GetNode(fileNodeId, cancellationToken);
          _ = await GetKeyTransformer(await GetNode(node.ParentNode, cancellationToken));

          Nodes.DeleteOne((record) => record.Id == fileNodeId, cancellationToken: cancellationToken);
        }, cancellationToken: cancellationToken));
      }
    }
  }
}

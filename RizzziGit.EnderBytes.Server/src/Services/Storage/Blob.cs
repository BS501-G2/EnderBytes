using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using System.Collections.Generic;
using System.Threading;
using Framework.Memory;
using Utilities;

public sealed partial class StorageService
{
  public sealed class BlobStorage(StorageService service, long ownerUserId, long id, long keySharedId) : Storage(service, ownerUserId, id, keySharedId)
  {
    private sealed record FileNodeKey(long FileNodeId, byte[] AesEncryptedKey, byte[] AesIv);

    public new sealed class Session(BlobStorage storage, ConnectionService.Connection connection, KeyService.Transformer.Key transformer) : Storage.Session(storage, connection, transformer)
    {
      public new readonly BlobStorage Storage = storage;

      private IMongoCollection<Node> NodeCollection => FileSystemDatabase.GetCollection<Node>();

      protected override Node Internal_FileNodeCreate((long Id, byte[] AesKey)? parentFolderNode, string name, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override void Internal_FileNodeHandleClose((long Id, byte[] AesKey) fileNode, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override long Internal_FileNodeHandleOpen((long Id, byte[] AesKey) fileNode, long? snapshotId, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override long Internal_FileNodeHandlePosition((long Id, byte[] AesKey) fileNode, long? newPosition, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override CompositeBuffer Internal_FileNodeHandleRead((long Id, byte[] AesKey) fileNode, long length, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override long Internal_FileNodeHandleSize((long Id, byte[] AesKey) fileNode, long? newSize, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override void Internal_FileNodeHandleWrite((long Id, byte[] AesKey) fileNode, long length, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override FileNodeSnapshot Internal_FileNodeSnapshotCreate((long Id, byte[] AesKey) fileNode, long? baseSnapshotId, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override IEnumerable<FileNodeSnapshot> Internal_FileNodeSnapshotList((long Id, byte[] AesKey) fileNode, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      // protected override Node Internal_FolderNodeCreate(long? parentFolderNodeId, string name, CancellationToken cancellationToken = default)
      // {
      //   lock (this)
      //   {
      //     Node? parentNode = parentFolderNodeId != null
      //       ? NodeCollection.FindOne((record) => record.Id == parentFolderNodeId, cancellationToken: cancellationToken) ?? throw new ArgumentException("Invalid parent node id.", nameof(parentFolderNodeId))
      //       : null;

      //     // (long id, long createTime, long updateTime) = NodeCollection.
      //     // Node node = new(StorageId, )

      //     MongoClient.RunTransaction((cancellationToken) =>
      //     {
      //     }, cancellationToken: cancellationToken);

      //     throw new NotImplementedException();
      //   }
      // }

      protected override Node Internal_FolderNodeCreate((long Id, byte[] AesKey)? parentFolderNode, string name, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          Node? parentNode = parentFolderNode != null
            ? NodeCollection.FindOne((record) => record.Id == parentFolderNode.Value.Id, cancellationToken: cancellationToken) ?? throw new ArgumentException("Invalid parent node id.", nameof(parentFolderNode))
            : null;

          return MongoClient.RunTransaction((cancellationToken) =>
          {
            (long id, long createTime, long updateTime) = NodeCollection.GenerateNewId(cancellationToken);
            (byte[] key, byte[] iv) = Server.KeyService.GetNewAesPair();
            Node node = new(StorageId, id, parentNode?.Id, createTime, updateTime, name, NodeType.Folder);
            NodeKey nodeKey = new(StorageId, node.Id, key, iv);

            NodeCollection.InsertOne(node, cancellationToken: cancellationToken);
            NodeKeyCollection.InsertOne(nodeKey, cancellationToken: cancellationToken);
            return node;
          }, cancellationToken: cancellationToken);
        }
      }

      protected override IEnumerable<Node> Internal_FolderNodeScan((long Id, byte[] AesKey)? folderNode, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          Node? scanNode = folderNode != null
            ? NodeCollection.FindOne((record) => record.Id == folderNode.Value.Id, cancellationToken: cancellationToken)
            : null;

          long? scanNodeId = scanNode?.Id;
          foreach (Node node in NodeCollection.Find((record) => record.ParentId == scanNodeId).ToEnumerable(cancellationToken))
          {
            yield return node;
          }
        }
      }

      protected override void Internal_NodeDelete((long Id, byte[] AesKey) node, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          Node? scanNode = NodeCollection.FindOne((record) => record.Id == node.Id, cancellationToken: cancellationToken);
        }
      }

      protected override Node Internal_NodeStat((long Id, byte[] AesKey) node, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override Node Internal_SymbolicLinkHandleCreate((long Id, byte[] AesKey)? parentFolderNode, string name, string[] target, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override string[] Internal_SymbolicLinkHandleTarget((long Id, byte[] AesKey) symbolicLinkNode, string[]? newTarget, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override Trash Internal_TrashItemCreate(long nodeId, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override void Internal_TrashItemDelete(long trashItemId, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override IEnumerable<Trash> Internal_TrashItemList(CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      protected override void Internal_TrashItemRestore(long trashItemId, CancellationToken cancellationToken = default)
      {
        throw new NotImplementedException();
      }

      // protected override Task<Node> Internal_FolderNodeCreate(long? parentFolderNodeId, string name, CancellationToken cancellationToken = default) => RunTask((cancellationToken) =>
      // {
      //   Node? parentFolderNode = parentFolderNodeId != null
      //     ? NodeCollection.FindOne((record) => record.Id == parentFolderNodeId, cancellationToken: cancellationToken) ?? throw new ArgumentException("Invalid parent folder id.", nameof(parentFolderNodeId))
      //     : null;

      //   (long id, long createTime, long updateTime) = NodeCollection.GenerateNewId(cancellationToken);
      //   Node folderNode = new(StorageId, id, parentFolderNodeId, createTime, updateTime, name, NodeType.Folder);

      //   NodeCollection.InsertOne(folderNode, cancellationToken: cancellationToken);
      //   return folderNode;
      // }, cancellationToken);

      // protected override Task<Node[]> Internal_FolderNodeScan(long? folderNodeId, CancellationToken cancellationToken = default) => RunTask((cancellationToken) => MongoClient.RunTransaction((cancellationToken) => NodeCollection.Find((node) => node.StorageId == StorageId && node.ParentId == folderNodeId).ToEnumerable(cancellationToken).ToArray(), cancellationToken: cancellationToken), cancellationToken);

      // protected override Task Internal_NodeDelete(long nodeId, CancellationToken  cancellationToken = default) => RunTask((cancellationToken) => MongoClient.RunTransaction((cancellationToken) =>
      // {
      //   NodeCollection.DeleteOne((record) => nodeId == record.Id, cancellationToken: cancellationToken);
      // }, cancellationToken: cancellationToken), cancellationToken);
    }

    protected override Storage.Session CreateSession(ConnectionService.Connection connection, KeyService.Transformer.Key transformer) => new Session(this, connection, transformer);
  }
}

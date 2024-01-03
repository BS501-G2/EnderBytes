using RizzziGit.Framework.Memory;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class StorageService
{
  public sealed class BlobStorage(StorageService service, long id, long keySharedId) : Storage(service, id, keySharedId)
  {
    public new sealed class Session(BlobStorage storage, ConnectionService.Connection connection, KeyService.Transformer.Key transformer) : Storage.Session(storage, connection, transformer)
    {
      public new readonly BlobStorage Storage = storage;

      protected override Task<Node> Internal_FileNodeCreate(long? parentFolderNodeId, string name)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_FileNodeHandleClose(long fileHandleId)
      {
        throw new NotImplementedException();
      }

      protected override Task<long> Internal_FileNodeHandleOpen(long fileNodeId, long? snapshotId)
      {
        throw new NotImplementedException();
      }

      protected override Task<long> Internal_FileNodeHandlePosition(long fileHandleId, long? newPosition)
      {
        throw new NotImplementedException();
      }

      protected override Task<CompositeBuffer> Internal_FileNodeHandleRead(long fileHandleId, long length)
      {
        throw new NotImplementedException();
      }

      protected override Task<long> Internal_FileNodeHandleSize(long fileHandleId, long? newSize)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_FileNodeHandleWrite(long fileHandleId, long length)
      {
        throw new NotImplementedException();
      }

      protected override Task<FileNodeSnapshot> Internal_FileNodeSnapshotCreate(long fileNodeId, long? baseSnapshotId)
      {
        throw new NotImplementedException();
      }

      protected override Task<FileNodeSnapshot[]> Internal_FileNodeSnapshotList(long fileNodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task<Node> Internal_FolderNodeCreate(long? parentFolderNodeId, string name)
      {
        throw new NotImplementedException();
      }

      protected override Task<Node[]> Internal_FolderNodeScan(long folderNodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_NodeDelete(long nodeId, string name)
      {
        throw new NotImplementedException();
      }

      protected override Task<Share> Internal_NodeShareCreate(long nodeId, long targetUserId, ShareType accessType)
      {
        throw new NotImplementedException();
      }

      protected override Task<bool> Internal_NodeShareDelete(long shareId)
      {
        throw new NotImplementedException();
      }

      protected override Task<Share[]> Internal_NodeShareList(long nodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_NodeStat(long nodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task<Node> Internal_SymbolicLinkHandleCreate(long? parentFolderNodeId, string name, string[] target)
      {
        throw new NotImplementedException();
      }

      protected override Task<string[]> Internal_SymbolicLinkHandleTarget(long symbolicLinkHandleId, string[]? newTarget)
      {
        throw new NotImplementedException();
      }

      protected override Task<Trash> Internal_TrashItemCreate(long nodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_TrashItemDelete(long trashItemId)
      {
        throw new NotImplementedException();
      }

      protected override Task<Trash[]> Internal_TrashItemList()
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_TrashItemRestore(long trashItemId)
      {
        throw new NotImplementedException();
      }
    }

    protected override Storage.Session CreateSession(ConnectionService.Connection connection, KeyService.Transformer.Key transformer) => new Session(this, connection, transformer);
  }
}

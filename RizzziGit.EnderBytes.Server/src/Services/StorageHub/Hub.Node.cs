namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

public sealed partial class StorageHubService
{
  public abstract partial class Hub : Lifetime
  {
    public abstract class Node
    {
      public abstract class File(Hub hub, long id, long keyId) : Node(hub, id, keyId)
      {
        public sealed class Snapshot(Hub hub, File file, long id, long baseSnapshotId)
        {
          public readonly Hub Hub = hub;
          public readonly File File = file;
          public readonly long SnapshotId = id;
          public readonly long? BaseSnapshotId = baseSnapshotId;

          public Task<Snapshot> CreateSnapshot(ConnectionService.IConnection connection) => File.CreateSnapshot(connection, SnapshotId);
        }

        protected abstract Task<Snapshot[]> Internal_ScanSnapshots(ConnectionService.IConnection connection, long? baseSnapshotId);
        protected abstract Task<Snapshot> Internal_CreateSnapshot(ConnectionService.IConnection connection);

        public Task<Snapshot[]> ScanSnapshots(ConnectionService.IConnection connection, long? baseSnapshotId = null) => Hub.RunTask((_) => Internal_ScanSnapshots(connection, baseSnapshotId));
        public Task<Snapshot> CreateSnapshot(ConnectionService.IConnection connection, long? baseSnapshotId = null) => Hub.RunTask(async (_) =>
        {
          if (baseSnapshotId == null && (await ScanSnapshots(connection)).Length != 0)
          {
            throw new InvalidOperationException("Must specify a snapshot id when the file has one or more existing snapshots.");
          }

          return await Internal_CreateSnapshot(connection);
        });
      }

      public abstract class Folder(Hub hub, long nodeId, long keyId) : Node(hub, nodeId, keyId)
      {
        protected abstract Task<Node[]> Internal_Scan(ConnectionService.IConnection connection);

        protected abstract Task<File> Internal_CreateFile(ConnectionService.IConnection connection, string name);
        protected abstract Task<Folder> Internal_CreateFolder(ConnectionService.IConnection connection, string name);
        protected abstract Task<SymbolicLink> Internal_CreateSymbolicLink(ConnectionService.IConnection connection, string name, string[] target);
      }

      public abstract class SymbolicLink(Hub hub, long nodeId, long keyId) : Node(hub, nodeId, keyId)
      {
        protected abstract Task Internal_Delete(ConnectionService.IConnection connection, string name);
      }

      private Node(Hub hub, long nodeId, long keyId)
      {
        Hub = hub;
        NodeId = nodeId;
        KeyId = keyId;
      }

      public readonly Hub Hub;
      public readonly long NodeId;
      public readonly long KeyId;
    }
  }
}

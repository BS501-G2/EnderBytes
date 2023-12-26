namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

public sealed partial class StorageHubService
{
  private long LastFileHandleId = 0;

  public abstract partial class Hub : Lifetime
  {
    public abstract class Node
    {
      public abstract class File(Hub hub, long id, long keyId) : Node(hub, id, keyId)
      {
        public abstract class Snapshot(Hub hub, File file, long id, long baseSnapshotId)
        {
          public readonly Hub Hub = hub;
          public readonly File File = file;
          public readonly long SnapshotId = id;
          public readonly long? BaseSnapshotId = baseSnapshotId;

          protected abstract Task<FileHandle> Internal_Open(KeyGeneratorService.Transformer.Key nodeKey, HubFileAccess access);

          public Task<Snapshot> CreateSnapshot() => File.CreateSnapshot(SnapshotId);
          public Task<FileHandle> Open(HubFileAccess access, HubFileMode mode) => Hub.RunTask(async (_) =>
          {
            foreach (FileHandle handle in Hub.FileHandles.Values.Where((handle) => handle.SnapshotId == SnapshotId))
            {
              if (handle.Access.HasFlag(HubFileAccess.Exclusive))
              {
                throw new InvalidOperationException("The current handle has exclusive access.");
              }
            }

            {
              FileHandle handle = await Internal_Open(nodeKey, access);
              return handle;
            }
          });
        }

        protected abstract Task<Snapshot[]> Internal_ScanSnapshots(long? baseSnapshotId);
        protected abstract Task<Snapshot> Internal_CreateSnapshot();

        public Task<Snapshot[]> ScanSnapshots(long? baseSnapshotId = null) => Hub.RunTask((_) => Internal_ScanSnapshots(baseSnapshotId));
        public Task<Snapshot> CreateSnapshot(long? baseSnapshotId = null) => Hub.RunTask(async (_) =>
        {
          if (baseSnapshotId == null && (await ScanSnapshots()).Length != 0)
          {
            throw new InvalidOperationException("Must specify a snapshot id when the file has one or more existing snapshots.");
          }

          return await Internal_CreateSnapshot();
        });
      }

      public abstract class Folder(Hub hub, long nodeId, long keyId) : Node(hub, nodeId, keyId)
      {
        protected abstract Task<Node[]> Internal_Scan();

        protected abstract Task<File> Internal_CreateFile(string name);
        protected abstract Task<Folder> Internal_CreateFolder(string name);
        protected abstract Task<SymbolicLink> Internal_CreateSymbolicLink(string name, string[] target);
      }

      public abstract class SymbolicLink(Hub hub, long nodeId, long keyId) : Node(hub, nodeId, keyId)
      {
        protected abstract Task Internal_Delete(ConnectionService.Connection connection, string name);
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

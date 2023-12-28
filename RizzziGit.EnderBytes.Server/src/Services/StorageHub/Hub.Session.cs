namespace RizzziGit.EnderBytes.Services;

using Framework.Memory;
using Framework.Services;

public sealed partial class StorageHubService
{
  public abstract partial class Hub : Lifetime
  {
    public sealed record Node(long Id, long KeySharedId, long CreateTime, long UpdateTime, string Name);
    public sealed record TrashEntry(long Id, long NodeId, long TrashTime, string[] OriginalPath);
    public sealed record FileSnapshot(long Id, long BaseId, long CreateTime, long UpdateTime, long NodeId, long AuthorUserId, long Size);

    public abstract partial class Session(Hub hub, ConnectionService.Connection connection) : Lifetime($"Hub session for {connection.Id}")
    {
      public readonly Hub Hub = hub;

      public long HubId => Hub.HubId;
      public Server Server => Hub.Server;
      public readonly ConnectionService.Connection Connection = connection;

      protected Task<KeyService.Transformer.Key> GetKeyTransformer(long KeySharedId) => Hub.RunTask((cancellationToken) => Hub.Server.KeyService.GetTransformer(Connection.Session?.Transformer, KeySharedId));

      // protected abstract Task<Node> Internal_NodeStat(long nodeId);
      protected abstract Task Internal_NodeDelete(long fileNodeId);

      // protected abstract Task<Node> Internal_NodeTrashRestore(long nodeId);
      // protected abstract Task<TrashEntry> Internal_NodeTrashCreate(long nodeId);
      // protected abstract Task<TrashEntry[]> Internal_NodeScan();

      protected abstract Task<Node> Internal_FolderNodeCreate(long? parentFolderNodeId, string name);
      protected abstract Task<Node[]> Internal_FolderNodeScan(long? folderNodeId);

      // protected abstract Task<Node> Internal_FileNodeCreate(long parentFolderNodeId, string name);

      // protected abstract Task<FileSnapshot> Internal_FileNodeSnapshotCreate(long nodeId, long? baseSnapshotId);
      // protected abstract Task<FileSnapshot[]> Internal_FileNodeSnapshotScan(long nodeId, long? baseSnapshotId);

      // protected abstract Task<long> Internal_FileNodeHandleOpen(long nodeId, FileAccess access, long snapshotId);
      // protected abstract Task<CompositeBuffer> Internal_FileNodeHandleRead(long handleId, long readSize);
      // protected abstract Task Internal_FileNodeHandleWrite(long handleId, CompositeBuffer buffer);
      // protected abstract Task Internal_FileNodeHandleSeek(long handleId, long position);
      // protected abstract Task Internal_FileNodeHandleSetSize(long handleId, long size);
      // protected abstract Task Internal_FileNodeHandleClose(long handleId);
    }
  }
}

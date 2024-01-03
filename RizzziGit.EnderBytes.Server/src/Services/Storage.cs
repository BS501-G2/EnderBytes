using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Framework.Memory;
using Framework.Collections;
using Framework.Services;
using Records;
using Utilities;

public sealed partial class StorageService(Server server) : Server.SubService(server, "Storage Service")
{
  public enum NodeType : byte { File, Folder, SymbolicLink }
  public enum StorageType : byte { Blob }
  public enum ShareType : byte { ReadOnly, ReadWrite, ManageShares }

  [Flags]
  public enum FileAccess : byte
  {
    Read = 1 << 0,
    Write = 1 << 1,
    Exclusive = 1 << 2,

    ReadWrite = Read | Write,
    ExclusiveReadWrite = Exclusive | ReadWrite
  }

  [Flags]
  public enum FileMode : byte
  {
    TruncateToZero = 1 << 0,
    Append = 1 << 1
  }

  public sealed record Node(
    long Id,
    long? ParentId,
    long CreateTime,
    long UpdateTime,
    string Name,
    byte[] AesKey,
    byte[] AesIv
  );

  public sealed record FileNodeSnapshot(
    long Id,
    long? ParentId,
    long CreateTime,
    long UpdateTime,
    long AuthorUserId
  );

  public sealed record Trash(
    long Id,
    long CreateTime,
    long TargetNodeId,
    string[] OriginalPath
  );

  public sealed record Share(
    long Id,
    long CreateTime,
    long TargetUserId,
    long TargetNodeId,
    byte[] AesKey
  );

  public abstract class Storage(StorageService service, long id, long keySharedId) : Lifetime($"#{id}")
  {
    public abstract class Session(Storage storage, ConnectionService.Connection connection, KeyService.Transformer.Key transformer) : Lifetime("Session")
    {
      public readonly Storage Storage = storage;
      public readonly ConnectionService.Connection Connection = connection;
      public readonly KeyService.Transformer.Key Transformer = transformer;

      public long StorageId => Storage.Id;
      public long? UserId => Connection.Session?.UserId;

      protected override async Task OnRun(CancellationToken cancellationToken)
      {
        try
        {
          await base.OnRun(cancellationToken);
        }
        finally
        {
          await Storage.RunTask((cancellationToken) =>
          {
            Storage.Sessions.Remove(Connection);

            return Task.CompletedTask;
          }, CancellationToken.None);
        }
      }

      protected abstract Task<Share> Internal_NodeShareCreate(long nodeId, long targetUserId, ShareType accessType);
      protected abstract Task<Share[]> Internal_NodeShareList(long nodeId);
      protected abstract Task<bool> Internal_NodeShareDelete(long shareId);

      protected abstract Task Internal_NodeDelete(long nodeId, string name);
      protected abstract Task Internal_NodeStat(long nodeId);

      protected abstract Task<Trash[]> Internal_TrashItemList();
      protected abstract Task<Trash> Internal_TrashItemCreate(long nodeId);
      protected abstract Task Internal_TrashItemRestore(long trashItemId);
      protected abstract Task Internal_TrashItemDelete(long trashItemId);

      protected abstract Task<Node> Internal_FolderNodeCreate(long? parentFolderNodeId, string name);
      protected abstract Task<Node[]> Internal_FolderNodeScan(long folderNodeId);

      protected abstract Task<Node> Internal_FileNodeCreate(long? parentFolderNodeId, string name);
      protected abstract Task<FileNodeSnapshot[]> Internal_FileNodeSnapshotList(long fileNodeId);
      protected abstract Task<FileNodeSnapshot> Internal_FileNodeSnapshotCreate(long fileNodeId, long? baseSnapshotId);

      protected abstract Task<long> Internal_FileNodeHandleOpen(long fileNodeId, long? snapshotId);
      protected abstract Task Internal_FileNodeHandleClose(long fileHandleId);
      protected abstract Task<CompositeBuffer> Internal_FileNodeHandleRead(long fileHandleId, long length);
      protected abstract Task Internal_FileNodeHandleWrite(long fileHandleId, long length);
      protected abstract Task<long> Internal_FileNodeHandlePosition(long fileHandleId, long? newPosition);
      protected abstract Task<long> Internal_FileNodeHandleSize(long fileHandleId, long? newSize);

      protected abstract Task<Node> Internal_SymbolicLinkHandleCreate(long? parentFolderNodeId, string name, string[] target);
      protected abstract Task<string[]> Internal_SymbolicLinkHandleTarget(long symbolicLinkHandleId, string[]? newTarget);
    }

    public readonly StorageService Service = service;
    public readonly long Id = id;
    public readonly long KeySharedId = keySharedId;

    public Server Server => Service.Server;

    private readonly WaitQueue<(TaskCompletionSource<Session> source, Storage storage, ConnectionService.Connection connection, CancellationToken cancellationToken)> WaitQueue = new(0);
    private readonly WeakDictionary<ConnectionService.Connection, Session> Sessions = [];

    protected abstract Session CreateSession(ConnectionService.Connection connection, KeyService.Transformer.Key transformer);

    public async Task<Session> GetSession(ConnectionService.Connection connection, CancellationToken cancellationToken = default)
    {
      TaskCompletionSource<Session> source = new();

      await WaitQueue.Enqueue((source, this, connection, cancellationToken), cancellationToken);
      return await source.Task;
    }

    protected override async Task OnRun(CancellationToken serviceCancellationToken)
    {
      try
      {
        await foreach (var (source, storage, connection, cancellationToken) in WaitQueue.WithCancellation(serviceCancellationToken))
        {
          using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceCancellationToken, cancellationToken);

          try
          {
            source.SetResult(await RunTask(async (cancellationToken) =>
            {
              if (!Sessions.TryGetValue(connection, out Session? session))
              {
                KeyService.Transformer.Key key = await Server.KeyService.GetTransformer(connection.Session?.Transformer, storage.KeySharedId, cancellationToken);

                session = CreateSession(connection, key);

                Sessions.Add(connection, session);
                session.Start(serviceCancellationToken);
              }

              return session;
            }, linkedCancellationTokenSource.Token));
          }
          catch (Exception exception)
          {
            source.SetException(exception);
          }
        }
      }
      finally
      {
        await Service.RunTask((cancellationToken) =>
        {
          Service.StorageLifetimes.Remove(Id);

          return Task.CompletedTask;
        }, CancellationToken.None);
      }
    }
  }

  private IMongoDatabase MainDatabase => Server.MainDatabase;
  private IMongoCollection<Record.Storage> StorageCollection => MainDatabase.GetCollection<Record.Storage>();

  private readonly WaitQueue<(TaskCompletionSource<Storage> source, long storageId, CancellationToken cancellationToken)> WaitQueue = new(0);
  private readonly WeakDictionary<long, Storage> StorageLifetimes = [];

  public async Task<Storage> GetStorage(long storageId, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<Storage> source = new();

    await WaitQueue.Enqueue((source, storageId, cancellationToken), cancellationToken);
    return await source.Task;
  }

  public async Task<Storage.Session> GetStorageSession(long storageId, ConnectionService.Connection connection, CancellationToken cancellationToken = default)
  {
    Storage storage = await GetStorage(storageId, cancellationToken);
    Storage.Session session = await storage.GetSession(connection, cancellationToken);
    return session;
  }

  protected override async Task OnRun(CancellationToken serviceCancellationToken)
  {
    await foreach (var (source, storageId, cancellationToken) in WaitQueue.WithCancellation(serviceCancellationToken))
    {
      using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceCancellationToken, cancellationToken);

      try
      {
        source.SetResult(await RunTask((cacnellationToken) =>
        {
          if (!StorageLifetimes.TryGetValue(storageId, out Storage? storage))
          {
            Record.Storage record = StorageCollection.FindOne((record) => record.Id == storageId, cancellationToken: cancellationToken) ?? throw new ArgumentException(null, nameof(storageId));

            storage = record.Type switch
            {
              StorageType.Blob => new BlobStorage(this, record.Id, record.KeySharedId),

              _ => throw new InvalidOperationException("Invalid storage type.")
            };

            StorageLifetimes.AddOrUpdate(storageId, storage);
            storage.Start(serviceCancellationToken);
          }

          return Task.FromResult(storage);
        }, linkedCancellationTokenSource.Token));
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

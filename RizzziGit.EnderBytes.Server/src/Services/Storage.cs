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
  public enum UserAccess
  {
    None = 0,
    CanRead = 1 << 0,
    CanWrite = 1 << 1,
    CanManageShares = 1 << 2,
    CanManageFile = 1 << 3
  }

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
    long StorageId,
    long Id,
    long? ParentId,
    long CreateTime,
    long UpdateTime,
    string Name,
    NodeType Type
  );

  public sealed record NodeKey(
    long StorageId,
    long NodeId,
    byte[] AesKey,
    byte[] AesIv
  );

  public sealed record FileNodeSnapshot(
    long StorageId,
    long Id,
    long? ParentId,
    long CreateTime,
    long UpdateTime,
    long AuthorUserId
  );

  public sealed record Trash(
    long StorageId,
    long Id,
    long CreateTime,
    long TargetNodeId,
    string[] OriginalPath
  );

  public sealed record Share(
    long StorageId,
    long Id,
    long CreateTime,
    long? TargetUserId,
    long TargetNodeId,
    byte[] AesKey,
    ShareType Type
  );

  public abstract class Storage(StorageService service, long ownerUserId, long id, long keySharedId) : Lifetime($"#{id}")
  {
    public abstract class Session(Storage storage, ConnectionService.Connection connection, KeyService.Transformer.Key transformer) : Lifetime("Session")
    {
      public Server Server => Storage.Server;

      public readonly Storage Storage = storage;
      public readonly ConnectionService.Connection Connection = connection;
      public readonly KeyService.Transformer.Key Transformer = transformer;

      public long StorageId => Storage.Id;
      public long KeySharedId => Storage.KeySharedId;
      public long OwnerUserId => Storage.OwnerUserId;
      public long? CurrentUserId => Connection.Session?.UserId;

      public KeyService.AesPair GetNewAesPair() => Server.KeyService.GetNewAesPair();

      protected IMongoClient MongoClient => Server.MongoClient;
      protected IMongoDatabase MainDatabase => Server.MainDatabase;
      protected IMongoDatabase FileSystemDatabase => Server.MongoClient.GetDatabase("EnderBytesFileSystem");

      protected IMongoCollection<NodeKey> NodeKeyCollection => FileSystemDatabase.GetCollection<NodeKey>();
      protected IMongoCollection<Share> ShareCollection => FileSystemDatabase.GetCollection<Share>();

      protected NodeKey? GetFileNodeKey(Node node, CancellationToken cancellationToken = default)
      {
        if (node.Type != NodeType.File)
        {
          throw new ArgumentException("Invalid node type.", nameof(node));
        }

        lock (this)
        {
          return MongoClient.RunTransaction((cancellationToken) => NodeKeyCollection.FindOne((record) => record.NodeId == node.Id, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
        }
      }

      protected NodeKey CreateFileNodeKey(Node node, CancellationToken cancellationToken = default)
      {
        if (node.Type != NodeType.File)
        {
          throw new ArgumentException("Invalid node type.", nameof(node));
        }

        lock (this)
        {
          (byte[] key, byte[] iv) = GetNewAesPair();

          return MongoClient.RunTransaction((cancellationToken) =>
          {
            NodeKey fileNodeKey = new(StorageId, node.Id, key, iv);

            NodeKeyCollection.InsertOne(fileNodeKey, cancellationToken: cancellationToken);
            return fileNodeKey;
          }, cancellationToken: cancellationToken);
        }
      }

      protected override async Task OnRun(CancellationToken cancellationToken)
      {
        try
        {
          cancellationToken.ThrowIfCancellationRequested();
          lock (Storage)
          {
            Storage.Sessions.Add(Connection, this);
          }

          await base.OnRun(cancellationToken);
        }
        finally
        {
          lock (Storage)
          {
            Storage.Sessions.Remove(Connection);
          }
        }
      }

      protected Share Internal_NodeShareCreate(long nodeId, long? targetUserId, ShareType type, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          if (targetUserId == null && type == ShareType.ManageShares)
          {
            throw new ArgumentNullException(nameof(targetUserId), "Non-logged in user cannot manage shares.");
          }

          byte[] aesKey = CurrentUserId == OwnerUserId
            ? Connection.Session!.GetKeyTransformer(KeySharedId).Decrypt((NodeKeyCollection.FindOne(
                (record) => record.NodeId == nodeId,
                cancellationToken: cancellationToken
              ) ?? throw new ArgumentException("Invalid node id.", nameof(nodeId))).AesKey)
            : Connection.Session!.Transformer.Decrypt((ShareCollection.FindOne(
                (record) =>
                  record.TargetNodeId == nodeId &&
                  record.TargetUserId == CurrentUserId &&
                  record.Type == ShareType.ManageShares,
                cancellationToken: cancellationToken
              ) ?? throw new ArgumentException("Invalid access.", nameof(nodeId))).AesKey);

          return MongoClient.RunTransaction((cancellationToken) =>
          {
            (long id, long createTime, _) = ShareCollection.GenerateNewId(cancellationToken);
            Share share = new(StorageId, id, createTime, targetUserId, nodeId, targetUserId != null ? Connection.Session!.Transformer.Encrypt(aesKey) : aesKey, type);
            ShareCollection.InsertOne(share, cancellationToken: cancellationToken);
            return share;
          }, cancellationToken: cancellationToken);
        }
      }

      protected IEnumerable<Share> Internal_NodeShareList(long nodeId, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          if (!GetAccessFlags(nodeId, cancellationToken).HasFlag(UserAccess.CanManageShares))
          {
            throw new ArgumentException("Invalid access.", nameof(nodeId));
          }

          return MongoClient.RunTransaction((cancellationToken) => ShareCollection.Find(
            (record) =>
              record.StorageId == StorageId &&
              record.TargetNodeId == nodeId &&
              (OwnerUserId == CurrentUserId || CurrentUserId == record.TargetUserId)
          ).ToEnumerable(cancellationToken: cancellationToken), cancellationToken: cancellationToken);
        }
      }

      protected bool Internal_NodeShareDelete(long nodeId, long shareId, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          if (!GetAccessFlags(nodeId, cancellationToken).HasFlag(UserAccess.CanManageShares))
          {
            throw new ArgumentException("Invalid access.", nameof(nodeId));
          }

          return MongoClient.RunTransaction((cancellationToken) => ShareCollection.DeleteOne((record) => record.TargetNodeId == nodeId && record.Id == shareId, cancellationToken).DeletedCount != 0, cancellationToken: cancellationToken);
        }
      }

      protected UserAccess GetAccessFlags(long? nodeId, CancellationToken cancellationToken = default)
      {
        UserAccess access = UserAccess.None;

        if (OwnerUserId == CurrentUserId)
        {
          access |= UserAccess.CanRead | UserAccess.CanWrite | UserAccess.CanManageShares | UserAccess.CanManageFile;
        }
        else
        {
          Share share = MongoClient.RunTransaction((cancellationToken) => ShareCollection.Find((record) => record.TargetNodeId == nodeId && record.TargetUserId == CurrentUserId).SortBy((record) => record.Type).FirstOrDefault(cancellationToken), cancellationToken: cancellationToken);

          if (share.Type >= ShareType.ReadOnly)
          {
            access |= UserAccess.CanRead;
          }

          if (share.Type >= ShareType.ReadWrite)
          {
            access |= UserAccess.CanWrite;
          }

          if (share.Type >= ShareType.ManageShares)
          {
            access |= UserAccess.CanManageShares;
          }
        }

        return access;
      }

      protected abstract void Internal_NodeDelete((long Id, byte[] AesKey) node, CancellationToken cancellationToken = default);
      protected abstract Node Internal_NodeStat((long Id, byte[] AesKey) node, CancellationToken cancellationToken = default);

      protected abstract IEnumerable<Trash> Internal_TrashItemList(CancellationToken cancellationToken = default);
      protected abstract Trash Internal_TrashItemCreate(long nodeId, CancellationToken cancellationToken = default);
      protected abstract void Internal_TrashItemRestore(long trashItemId, CancellationToken cancellationToken = default);
      protected abstract void Internal_TrashItemDelete(long trashItemId, CancellationToken cancellationToken = default);

      protected abstract Node Internal_FolderNodeCreate((long Id, byte[] AesKey)? parentFolderNode, string name, CancellationToken cancellationToken = default);
      protected abstract IEnumerable<Node> Internal_FolderNodeScan((long Id, byte[] AesKey)? folderNode, CancellationToken cancellationToken = default);

      protected abstract Node Internal_FileNodeCreate((long Id, byte[] AesKey)? parentFolderNode, string name, CancellationToken cancellationToken = default);
      protected abstract IEnumerable<FileNodeSnapshot> Internal_FileNodeSnapshotList((long Id, byte[] AesKey) fileNode, CancellationToken cancellationToken = default);
      protected abstract FileNodeSnapshot Internal_FileNodeSnapshotCreate((long Id, byte[] AesKey) fileNode, long? baseSnapshotId, CancellationToken cancellationToken = default);

      protected abstract long Internal_FileNodeHandleOpen((long Id, byte[] AesKey) fileNode, long? snapshotId, CancellationToken cancellationToken = default);
      protected abstract void Internal_FileNodeHandleClose((long Id, byte[] AesKey) fileNode, CancellationToken cancellationToken = default);
      protected abstract CompositeBuffer Internal_FileNodeHandleRead((long Id, byte[] AesKey) fileNode, long length, CancellationToken cancellationToken = default);
      protected abstract void Internal_FileNodeHandleWrite((long Id, byte[] AesKey) fileNode, long length, CancellationToken cancellationToken = default);
      protected abstract long Internal_FileNodeHandlePosition((long Id, byte[] AesKey) fileNode, long? newPosition, CancellationToken cancellationToken = default);
      protected abstract long Internal_FileNodeHandleSize((long Id, byte[] AesKey) fileNode, long? newSize, CancellationToken cancellationToken = default);

      protected abstract Node Internal_SymbolicLinkHandleCreate((long Id, byte[] AesKey)? parentFolderNode, string name, string[] target, CancellationToken cancellationToken = default);
      protected abstract string[] Internal_SymbolicLinkHandleTarget((long Id, byte[] AesKey) symbolicLinkNode, string[]? newTarget, CancellationToken cancellationToken = default);
    }

    public readonly StorageService Service = service;
    public readonly long Id = id;
    public readonly long KeySharedId = keySharedId;
    public readonly long OwnerUserId = ownerUserId;

    public Server Server => Service.Server;

    private readonly WeakDictionary<ConnectionService.Connection, Session> Sessions = [];

    protected abstract Session CreateSession(ConnectionService.Connection connection, KeyService.Transformer.Key transformer);

    public Session GetSession(ConnectionService.Connection connection, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        if (!Sessions.TryGetValue(connection, out Session? session))
        {
          KeyService.Transformer.Key key = Server.KeyService.GetTransformer(connection.Session?.Transformer, KeySharedId, cancellationToken);

          session = CreateSession(connection, key);
          session.Start(cancellationToken);
        }

        return session;
      }
    }

    protected override async Task OnRun(CancellationToken serviceCancellationToken)
    {
      try
      {
        serviceCancellationToken.ThrowIfCancellationRequested();
        lock (Service)
        {
          Service.StorageLifetimes.Add(Id, this);
        }

        await base.OnRun(serviceCancellationToken);
      }
      finally
      {
        lock (Service)
        {
          Service.StorageLifetimes.Remove(Id);
        }
      }
    }
  }

  private IMongoDatabase MainDatabase => Server.MainDatabase;
  private IMongoCollection<Record.Storage> StorageCollection => MainDatabase.GetCollection<Record.Storage>();

  private readonly WeakDictionary<long, Storage> StorageLifetimes = [];

  public Storage GetStorage(long storageId, CancellationToken cancellationToken = default)
  {
    lock (this)
    {
      if (!StorageLifetimes.TryGetValue(storageId, out Storage? storage))
      {
        Record.Storage record = StorageCollection.FindOne((record) => record.Id == storageId, cancellationToken: cancellationToken) ?? throw new ArgumentException(null, nameof(storageId));

        storage = record.Type switch
        {
          StorageType.Blob => new BlobStorage(this, record.OwnerUserId, record.Id, record.KeySharedId),

          _ => throw new InvalidOperationException("Invalid storage type.")
        };

        StorageLifetimes.AddOrUpdate(storageId, storage);
        storage.Start(cancellationToken);
      }

      return storage;
    }
  }

  public Storage.Session GetStorageSession(long storageId, ConnectionService.Connection connection, CancellationToken cancellationToken = default)
  {
    lock (this)
    {
      return GetStorage(storageId, cancellationToken).GetSession(connection, cancellationToken);
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

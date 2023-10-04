using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class VirtualStorageNodeResource(VirtualStorageNodeResource.ResourceManager manager, VirtualStorageNodeResource.ResourceData data) : Resource<VirtualStorageNodeResource.ResourceManager, VirtualStorageNodeResource.ResourceData, VirtualStorageNodeResource>(manager, data)
{
  public const string NAME = "VirtualStorageMetadata";
  public const int VERSION = 1;

  private const string KEY_STORAGE_POOL_ID = "PoolID";
  private const string KEY_NAME = "Name";
  private const string KEY_PARENT_NODE_ID = "ParentNodeID";
  private const string KEY_TYPE = "Type";
  private const string KEY_MODE = "Mode";
  private const string KEY_OWNER_USER_ID = "OwnerUserID";
  private const string KEY_ACCESS_TIME = "AccessTime";
  private const string KEY_BLOB_COUNT = "BlobCount";

  private const string INDEX_STORAGE_POOL_ID = $"Index_{NAME}_{KEY_STORAGE_POOL_ID}";

  public const int TYPE_FILE = 0;
  public const int TYPE_FOLDER = 1;
  public const int TYPE_SYMBOLIC_LINK = 2;

  public const int MODE_OTHERS_EXECUTE = 1 << 0;
  public const int MODE_OTHERS_WRITE = 1 << 1;
  public const int MODE_OTHERS_READ = 1 << 2;

  public const int MODE_GROUP_EXECUTE = 1 << 3;
  public const int MODE_GROUP_WRITE = 1 << 4;
  public const int MODE_GROUP_READ = 1 << 5;

  public const int MODE_USER_EXECUTE = 1 << 6;
  public const int MODE_USER_WRITE = 1 << 7;
  public const int MODE_USER_READ = 1 << 8;

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    ulong storagePoolId,
    string name,
    ulong? parentNodeId,
    byte type,
    int mode,
    ulong ownerUserId,
    ulong accessTime,
    ulong blobCount
  ) : Resource<ResourceManager, ResourceData, VirtualStorageNodeResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong StoragePoolID = storagePoolId;
    public string Name = name;
    public ulong? ParentNodeID = parentNodeId;
    public byte Type = type;
    public int Mode = mode;
    public ulong OwnerUserID = ownerUserId;
    public ulong AccessTime = accessTime;
    public ulong BlobCount = blobCount;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      StoragePoolID = data.StoragePoolID;
      Name = data.Name;
      ParentNodeID = data.ParentNodeID;
      Type = data.Type;
      Mode = data.Mode;
      OwnerUserID = data.OwnerUserID;
      AccessTime = data.AccessTime;
      BlobCount = data.BlobCount;
    }
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, VirtualStorageNodeResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main) : base(main, VERSION, NAME)
    {
      main.StoragePools.ResourceDeleteHandlers.Add(async (connection, resource, cancellationToken) =>
      {
        if (resource.Type != StoragePoolResource.TYPE_VIRTUAL_POOL)
        {
          return;
        }

        await DbDelete(connection, new() { { KEY_STORAGE_POOL_ID, ("=", resource.ID) } }, cancellationToken);
      });
    }

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,
      (ulong)(long)reader[KEY_STORAGE_POOL_ID],
      (string)reader[KEY_NAME],
      reader[KEY_PARENT_NODE_ID] is DBNull ? null : (ulong)(long)reader[KEY_PARENT_NODE_ID],
      (byte)(long)reader[KEY_TYPE],
      (int)(long)reader[KEY_MODE],
      (uint)(long)reader[KEY_OWNER_USER_ID],
      (ulong)(long)reader[KEY_ACCESS_TIME],
      (ulong)(long)reader[KEY_BLOB_COUNT]
    );

    protected override VirtualStorageNodeResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_PARENT_NODE_ID} integer;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_TYPE} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_MODE} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_ACCESS_TIME} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_STORAGE_POOL_ID} on {NAME}({KEY_STORAGE_POOL_ID})", cancellationToken);
      }
    }
  }

  public ulong StoragePoolID => Data.StoragePoolID;
  public string Name => Data.Name;
  public ulong? ParentNodeID => Data.ParentNodeID;
  public byte Type => Data.Type;
  public int Mode => Data.Mode;
  public ulong OwnerUserID => Data.OwnerUserID;
  public ulong AccessTime => Data.AccessTime;
  public ulong BlobCount => Data.BlobCount;
}

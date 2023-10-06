using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobStorageFileResource(BlobStorageFileResource.ResourceManager manager, BlobStorageFileResource.ResourceData data) : Resource<BlobStorageFileResource.ResourceManager, BlobStorageFileResource.ResourceData, BlobStorageFileResource>(manager, data)
{
  public const string NAME = "VSFile";
  public const int VERSION = 1;

  private const string KEY_ACCESS_TIME = "AccessTime";
  private const string KEY_TRASH_TIME = "TrashTime";
  private const string KEY_STORAGE_POOL_ID = "StoragePoolID";
  private const string KEY_OWNER_USER_ID = "OwnerUserID";
  private const string KEY_FOLDER_ID = "FolderID";
  private const string KEY_NAME = "Name";
  private const string KEY_TYPE = "Type";
  private const string KEY_MODE = "Mode";
  private const string KEY_BUFFER_SIZE = "BufferSize";

  private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_NAME}_{KEY_STORAGE_POOL_ID}_{KEY_FOLDER_ID}";

  public const string JSON_KEY_ACCESS_TIME = "accessTime";
  public const string JSON_KEY_TRASH_TIME = "trashTime";
  public const string JSON_KEY_STORAGE_POOL_ID = "storagePoolId";
  public const string JSON_KEY_OWNER_USER_ID = "ownerUserId";
  public const string JSON_KEY_FOLDER_ID = "folderId";
  public const string JSON_KEY_NAME = "name";
  public const string JSON_KEY_TYPE = "type";
  public const string JSON_KEY_MODE = "mode";
  public const string JSON_KEY_BUFFER_SIZE = "bufferSize";

  public const byte TYPE_FILE = 0;
  public const byte TYPE_FOLDER = 1;
  public const byte TYPE_SYMBOLIC_LINK = 2;

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
    long createTime,
    long updateTime,
    long accessTime,
    long? trashTime,
    ulong storagePoolId,
    ulong ownerUserId,
    ulong? folderId,
    string name,
    byte type,
    int mode,
    long bufferSize
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileResource>.ResourceData(id, createTime, updateTime)
  {
    public long AccessTime = accessTime;
    public long? TrashTime = trashTime;
    public ulong StoragePoolID = storagePoolId;
    public ulong OwnerUserID = ownerUserId;
    public ulong? FolderID = folderId;
    public string Name = name;
    public byte Type = type;
    public int Mode = mode;
    public long BufferSize = bufferSize;

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_ACCESS_TIME, AccessTime },
        { JSON_KEY_TRASH_TIME, TrashTime },
        { JSON_KEY_STORAGE_POOL_ID, StoragePoolID },
        { JSON_KEY_OWNER_USER_ID, OwnerUserID },
        { JSON_KEY_FOLDER_ID, FolderID },
        { JSON_KEY_NAME, Name },
        { JSON_KEY_TYPE, Type },
        { JSON_KEY_MODE, Mode },
        { JSON_KEY_BUFFER_SIZE, BufferSize }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, BlobStorageFileResource>.ResourceManager(main, VERSION, NAME)
  {
    public static void ValidateFileType(in byte type)
    {
      if (!(
        (type == TYPE_FILE) ||
        (type == TYPE_FOLDER) ||
        (type == TYPE_SYMBOLIC_LINK)
      ))
      {
        throw new ArgumentException("Invalid type.", nameof(type));
      }
    }

    public static void ValidateFolder(in BlobStorageFileResource? folder)
    {
      if (folder != null && folder.Type != TYPE_FOLDER)
      {
        throw new ArgumentException("Invalid type.", nameof(folder));
      }
    }

    public static int SanitizeMode(in int mode) => mode & (
      MODE_OTHERS_READ | MODE_OTHERS_WRITE | MODE_OTHERS_EXECUTE |
      MODE_GROUP_READ | MODE_GROUP_WRITE | MODE_GROUP_EXECUTE |
      MODE_USER_READ | MODE_USER_WRITE | MODE_USER_EXECUTE
    );

    public static void ValidateStoragePoolType(in StoragePoolResource storagePool)
    {
      if (storagePool.Type != StoragePoolResource.TYPE_VIRTUAL_POOL)
      {
        throw new ArgumentException("Invalid type.", nameof(storagePool));
      }
    }

    public static int ValidateBufferSize(in int bufferSize)
    {
      if (bufferSize <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bufferSize));
      }

      return bufferSize;
    }

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_ACCESS_TIME],
      (long)reader[KEY_TRASH_TIME],
      (ulong)(long)reader[KEY_STORAGE_POOL_ID],
      (ulong)(long)reader[KEY_OWNER_USER_ID],
      reader[KEY_FOLDER_ID] is DBNull ? null : (ulong)(long)reader[KEY_FOLDER_ID],
      (string)reader[KEY_NAME],
      (byte)(long)reader[KEY_TYPE],
      (int)(long)reader[KEY_MODE],
      (long)reader[KEY_BUFFER_SIZE]
    );

    protected override BlobStorageFileResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_ACCESS_TIME} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_TRASH_TIME} integer;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_FOLDER_ID} integer;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_TYPE} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_MODE} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BUFFER_SIZE} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_UNIQUENESS} on {NAME}({KEY_NAME},{KEY_STORAGE_POOL_ID},{KEY_FOLDER_ID});", cancellationToken);
      }
    }

    public async Task<BlobStorageFileResource> Create(
      SQLiteConnection connection,
      StoragePoolResource storagePool,
      UserResource owner,
      BlobStorageFileResource? folder,
      byte type,
      string name,
      int mode,
      int bufferSize,
      CancellationToken cancellationToken
    )
    {
      ValidateFileType(type);
      ValidateStoragePoolType(storagePool);
      ValidateBufferSize(bufferSize);

      return await DbInsert(connection, new()
      {
        { KEY_ACCESS_TIME, GenerateTimestamp() },
        { KEY_TRASH_TIME, null },
        { KEY_STORAGE_POOL_ID, storagePool.ID },
        { KEY_OWNER_USER_ID, owner.ID },
        { KEY_FOLDER_ID, folder?.ID },
        { KEY_NAME, name },
        { KEY_TYPE, type },
        { KEY_MODE, SanitizeMode(mode) },
        { KEY_BUFFER_SIZE, bufferSize }
      }, cancellationToken);
    }

    public async Task Update(
      SQLiteConnection connection,
      BlobStorageFileResource file,
      long? trashTime,
      UserResource owner,
      BlobStorageFileResource? folder,
      byte type,
      string name,
      int mode,
      CancellationToken cancellationToken
    )
    {
      ValidateFileType(type);

      await DbUpdate(connection, new() { { KEY_ID, ("=", file.ID) } }, new()
      {
        { KEY_TRASH_TIME, trashTime },
        { KEY_OWNER_USER_ID, owner.ID },
        { KEY_FOLDER_ID, folder?.ID },
        { KEY_TYPE, type },
        { KEY_NAME, name },
        { KEY_MODE, SanitizeMode(mode) }
      }, cancellationToken);
    }
  }

  public long AccessTime => Data.AccessTime;
  public long? TrashTime => Data.TrashTime;
  public ulong StoragePoolID => Data.StoragePoolID;
  public ulong OwnerUserID => Data.OwnerUserID;
  public ulong? FolderID => Data.FolderID;
  public string Name => Data.Name;
  public byte Type => Data.Type;
  public int Mode => Data.Mode;
  public long BufferSize => Data.BufferSize;
}

using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Database;

public enum BlobStorageFileType : byte
{
  File, Directory, SymbolicLink
}

public sealed class BlobStorageFileResource(BlobStorageFileResource.ResourceManager manager, BlobStorageFileResource.ResourceData data) : Resource<BlobStorageFileResource.ResourceManager, BlobStorageFileResource.ResourceData, BlobStorageFileResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobStorageFileResource>.ResourceManager
  {
    public const string NAME = "BlobStorageFile";
    public const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_STORAGE_POOL_ID = "PoolId";
    private const string KEY_PARENT_FOLDER_ID = "ParentFolerId";
    private const string KEY_TYPE = "Type";
    private const string KEY_OWNER_USER_ID = "OwnerUserId";
    private const string KEY_NAME = "Name";

    private const string INDEX_NAME = $"Index_{NAME}_{KEY_NAME}";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.StoragePools.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_STORAGE_POOL_ID, ("=", resource.Id, null) }
      }, cancellationToken));

      OnResourceDelete((transaction, resource, cancellationToken) =>
      {
        if (resource.Type != BlobStorageFileType.Directory)
        {
          return Task.FromResult(0);
        }

        return DbDelete(transaction, new()
        {
          { KEY_PARENT_FOLDER_ID, ("=", resource.Id, null) }
        }, cancellationToken);
      });
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_ACCESS_TIME],
      reader[KEY_TRASH_TIME] is not DBNull ? (long)reader[KEY_TRASH_TIME] : null,
      (long)reader[KEY_STORAGE_POOL_ID],
      reader[KEY_PARENT_FOLDER_ID] is not DBNull ? (long)reader[KEY_PARENT_FOLDER_ID] : null,
      (BlobStorageFileType)(byte)(long)reader[KEY_TYPE],
      (long)reader[KEY_OWNER_USER_ID],
      (string)reader[KEY_NAME]
    );

    protected override BlobStorageFileResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TRASH_TIME} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PARENT_FOLDER_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(256) not null;");

        transaction.ExecuteNonQuery($"create index {INDEX_NAME} on {NAME}({KEY_NAME});");
      }
    }

    public BlobStorageFileResource? Get(
      DatabaseTransaction transaction,
      StoragePoolResource storagePool,
      BlobStorageFileResource? parentFolder,
      string name
    )
    {
      using var reader = DbSelect(transaction, new()
      {
        { KEY_STORAGE_POOL_ID, ("=", storagePool.Id, null) },
        { KEY_PARENT_FOLDER_ID, ("=", parentFolder?.Id, null) },
        { KEY_NAME, ("=", name, (storagePool.Flags & StoragePoolFlags.IgnoreCase) == StoragePoolFlags.IgnoreCase ? "nocase" : null) },
        { KEY_TRASH_TIME, ("=", null, null) }
      }, [], (1, null), null);

      while (reader.Read())
      {
        return Memory.ResolveFromData(CreateData(reader));
      }

      return null;
    }

    public BlobStorageFileResource Create(
      DatabaseTransaction transaction,
      StoragePoolResource storagePool,
      BlobStorageFileResource? parentFolder,
      BlobStorageFileType type,
      UserResource ownerUser,
      string name
    )
    {
      return DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
        { KEY_TRASH_TIME, null },
        { KEY_STORAGE_POOL_ID, storagePool.Id },
        { KEY_PARENT_FOLDER_ID, parentFolder?.Id },
        { KEY_TYPE, (byte)type },
        { KEY_OWNER_USER_ID, ownerUser.Id },
        { KEY_NAME, name }
      });
    }

    public async Task UpdateAccessTime(
      DatabaseTransaction transaction,
      BlobStorageFileResource file,
      long accessTime,
      CancellationToken cancellationToken
    )
    {
      await DbUpdate(transaction, new()
      {
        { KEY_ACCESS_TIME, accessTime }
      }, new()
      {
        { KEY_ID, ("=", file.Id, null) }
      }, cancellationToken);
    }

    public async Task Trash(DatabaseTransaction transaction, BlobStorageFileResource file, CancellationToken cancellationToken)
    {
      if (file.TrashTime != null)
      {
        return;
      }

      await DbUpdate(transaction, new()
      {
        { KEY_TRASH_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
      }, new()
      {
        { KEY_ID, ("=", file.Id, null) }
      }, cancellationToken);
    }

    public async Task Restore(
      DatabaseTransaction transaction,
      BlobStorageFileResource file,
      BlobStorageFileResource? parentFolder,
      string name,
      CancellationToken cancellationToken
    )
    {
      await DbUpdate(transaction, new()
      {
        { KEY_TRASH_TIME, null },
        { KEY_PARENT_FOLDER_ID, parentFolder },
        { KEY_NAME, name }
      }, new()
      {
        { KEY_ID, ("=", file.Id, null) },
      }, cancellationToken);
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long AccessTime,
    long? TrashTime,
    long StoragePoolId,
    long? ParentFolderId,
    BlobStorageFileType Type,
    long OwnerUserId,
    string Name
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_ACCESS_TIME = "accessTime";
    public const string KEY_TRASH_TIME = "trashTime";
    public const string KEY_STORAGE_POOL_ID = "storagePoolId";
    public const string KEY_PARENT_FOLDER_ID = "parentFolderId";
    public const string KEY_TYPE = "type";
    public const string KEY_OWNER_USER_ID = "ownerUserId";
    public const string KEY_NAME = "name";

    [JsonPropertyName(KEY_ACCESS_TIME)] public long AccessTime = AccessTime;
    [JsonPropertyName(KEY_TRASH_TIME)] public long? TrashTime = TrashTime;
    [JsonPropertyName(KEY_STORAGE_POOL_ID)] public long StoragePoolId = StoragePoolId;
    [JsonPropertyName(KEY_PARENT_FOLDER_ID)] public long? ParentFolderId = ParentFolderId;
    [JsonPropertyName(KEY_TYPE)] public BlobStorageFileType Type = Type;
    [JsonPropertyName(KEY_OWNER_USER_ID)] public long OwnerUserId = OwnerUserId;
    [JsonPropertyName(KEY_NAME)] public string Name = Name;
  }

  public long AccessTime => Data.AccessTime;
  public long? TrashTime => Data.TrashTime;
  public long StoragePoolId => Data.StoragePoolId;
  public long? ParentFolderId => Data.ParentFolderId;
  public BlobStorageFileType Type => Data.Type;
  public long OwnerUserId => Data.OwnerUserId;
  public string Name => Data.Name;
}

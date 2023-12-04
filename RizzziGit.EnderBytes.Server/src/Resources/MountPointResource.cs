using Microsoft.Data.Sqlite;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using StoragePools;

public sealed class MountPointResource(MountPointResource.ResourceManager manager, MountPointResource.ResourceData data) : Resource<MountPointResource.ResourceManager, MountPointResource.ResourceData, MountPointResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, MountPointResource>.ResourceManager
  {
    public const string NAME = "MountPoint";
    public const int VERSION = 1;

    private const string KEY_USER_ID = "UserId";
    private const string KEY_STORAGE_POOL_ID = "StoragePoolId";
    private const string KEY_NAME = "Name";
    private const string KEY_PATH = "Path";

    private const string INDEX_UNIQUENESS = $"Index_{KEY_PATH}";

    public ResourceManager(Resources.ResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.StoragePools.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new()
      {
        { KEY_STORAGE_POOL_ID, ("=", resource.Id) }
      });
    }

    protected override MountPointResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_STORAGE_POOL_ID],
      (string)reader[KEY_NAME],
      ParsePath((byte[])reader[KEY_PATH])
    );

    private static StoragePool.Path ParsePath(byte[] rawBytes)
    {
      BsonDocument document;

      {
        using MemoryStream stream = new(rawBytes);
        using BsonBinaryReader reader = new(stream);
        document = reader.ToBsonDocument();
      }

      return new(document.ToBsonDocument().AsBsonArray.Select((entry) => entry.AsString).ToArray());
    }

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      byte[] test = [];

      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PATH} blob not null;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_PATH});");
      }
    }

    public MountPointResource Create(DatabaseTransaction transaction, StoragePoolResource storagePool, string name, StoragePool.Path path) => DbInsert(transaction, new()
    {
      { KEY_STORAGE_POOL_ID, storagePool.Id },
      { KEY_NAME, name },
      { KEY_PATH, BsonArray.Create(path).ToBson() }
    });

    public IEnumerable<MountPointResource> Stream(DatabaseTransaction transaction, UserResource user) => DbStream(transaction, new()
    {
      { KEY_USER_ID, ("=", user.Id) }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long StoragePoolId,
    string Name,
    StoragePool.Path Path
  ) : Resource<ResourceManager, ResourceData, MountPointResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long StoragePoolId => Data.StoragePoolId;
  public string Name => Data.Name;
  public StoragePool.Path Path => Data.Path;
}

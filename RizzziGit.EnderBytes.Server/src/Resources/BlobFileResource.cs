using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public enum BlobFileType : byte
{
  File, Folder, SymbolicLink
}

public sealed class BlobFileResource : Resource<BlobFileResource.ResourceManager, BlobFileResource.ResourceData, BlobFileResource>
{
  public BlobFileResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceManager
  {
    private const string NAME = "BlobFile";
    private const int VERSION = 1;

    private const string KEY_POOL_ID = "BlobStoragePoolId";
    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_OWNER_USER_ID = "OwnerUserId";
    private const string KEY_TYPE = "Type";
    private const string KEY_PARENT_ID = "ParentId";
    private const string KEY_NAME = "Name";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobFileResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader[KEY_ACCESS_TIME] is DBNull ? null : (long)reader[KEY_ACCESS_TIME],
      reader[KEY_TRASH_TIME] is DBNull ? null : (long)reader[KEY_TRASH_TIME],
      (long)reader[KEY_POOL_ID],
      reader[KEY_PARENT_ID] is DBNull ? null : (long)reader[KEY_PARENT_ID],
      (byte)reader[KEY_TYPE],
      reader[KEY_PARENT_ID] is DBNull ? null : (long)reader[KEY_PARENT_ID],
      (string)reader[KEY_NAME]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TRASH_TIME} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PARENT_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;");
      }
    }

    public BlobFileResource? GetByName(DatabaseTransaction transaction, StoragePoolResource storagePool, BlobFileResource? parentFolder, string name)
    {
      foreach (BlobFileResource file in DbStream(transaction, new()
      {
        { KEY_POOL_ID, ("=", storagePool.Id, null) },
        { KEY_PARENT_ID, ("=", parentFolder?.Id, null) },
        { KEY_NAME, ("=", name, (storagePool.Flags & StoragePoolFlags.IgnoreCase) != 0 ? "nocase" : null) }
      }))
      {
        return file;
      }

      return null;
    }

    public (BlobFileResource file, BlobFileKeyResource fileKey) CreateFile(DatabaseTransaction transaction, StoragePoolResource storagePool, BlobFileResource? parentFolder, UserResource owner, UserAuthenticationResource userAuthentication, BlobFileType type, string name, byte[] hashCache)
    {
      BlobFileResource file = DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, null },
        { KEY_TRASH_TIME, null },
        { KEY_POOL_ID, storagePool.Id },
        { KEY_OWNER_USER_ID, owner?.Id },
        { KEY_TYPE, (byte)type },
        { KEY_PARENT_ID, parentFolder?.Id },
        { KEY_NAME, name }
      });

      return (file, Main.FileKeys.Create(transaction, file, userAuthentication, hashCache));
    }

    public async Task UpdateOwner(DatabaseTransaction transaction, BlobFileResource file, UserResource user, CancellationToken cancellationToken)
    {
      await DbUpdate(transaction, new()
      {
        { KEY_OWNER_USER_ID, user.Id }
      }, new()
      {
        { KEY_ID, ("=", file.Id, null) }
      }, cancellationToken);
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? AccessTime,
    long? TrashTime,
    long PoolId,
    long? OwnerUserId,
    byte Type,
    long? ParentId,
    string Name
  ) : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? AccessTime => Data.AccessTime;
  public long? TrashTime => Data.TrashTime;
  public long PoolId => Data.PoolId;
  public long? OwnerUserId => Data.OwnerUserId;
  public byte Type => Data.Type;
  public long? ParentId => Data.ParentId;
  public string Name => Data.Name;
}

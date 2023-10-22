using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class FSPFileResource(FSPFileResource.ResourceManager manager, FSPFileResource.ResourceData data) : Resource<FSPFileResource.ResourceManager, FSPFileResource.ResourceData, FSPFileResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FSPFileResource>.ResourceManager
  {
    public const string NAME = "FSPFile";
    public const int VERSION = 1;

    private const string KEY_STORAGE_POOL_ID = "PoolId";
    private const string KEY_OWNER = "OwnerUserId";
    private const string KEY_NAME = "Name";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(id, createTime, updateTime);

    protected override FSPFileResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_OWNER} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} integer not null;");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : Resource<ResourceManager, ResourceData, FSPFileResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
  }
}

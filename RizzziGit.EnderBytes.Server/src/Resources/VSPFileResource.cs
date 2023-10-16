using Microsoft.Data.Sqlite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

public sealed class FSPFileResource(FSPFileResource.ResourceManager manager, FSPFileResource.ResourceData data) : Resource<FSPFileResource.ResourceManager, FSPFileResource.ResourceData, FSPFileResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FSPFileResource>.ResourceManager
  {
    public const string NAME = "FSPFile";
    public const int VERSION = 1;

    private const string KEY_STORAGE_POOL_ID = "PoolId";
    private const string KEY_OWNER = "OwnerUserId";
    private const string KEY_NAME = "Name";
    private const string KEY_MODE = "Mode";

    public ResourceManager(MainResourceManager main, Database.Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(id, createTime, updateTime);

    protected override FSPFileResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
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

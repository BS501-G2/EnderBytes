using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobDataMapResource(BlobDataMapResource.ResourceManager manager, BlobDataMapResource.ResourceData data) : Resource<BlobDataMapResource.ResourceManager, BlobDataMapResource.ResourceData, BlobDataMapResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobDataMapResource>.ResourceManager
  {
    private const string NAME = "BlobMap";
    private const int VERSION = 1;

    private const string KEY_VERSION_ID = "VersionId";
    private const string KEY_DATA_ID = "DataId";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobDataMapResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, BlobDataMapResource>.ResourceData(Id, CreateTime, UpdateTime);
}

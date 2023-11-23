using Microsoft.Data.Sqlite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

public sealed class FileLinkResource : Resource<FileLinkResource.ResourceManager, FileLinkResource.ResourceData, FileLinkResource>
{
  public FileLinkResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceManager
  {
    private const string NAME = "FileLink";
    private const int VERSION = 1;

    private const string KEY_CONTENT_ID = "ContentId";
    private const string KEY_NODE_ID = "NodeId";
    private const string KEY_SNAPSHOT_ID = "SnapshotId";

    public ResourceManager(IMainResourceManager main, Database.Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime)
    {
      throw new NotImplementedException();
    }

    protected override FileLinkResource CreateResource(ResourceData data)
    {
      throw new NotImplementedException();
    }

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceData(Id, CreateTime, UpdateTime);
}

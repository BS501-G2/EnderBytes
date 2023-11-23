using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class FileContentResource(FileContentResource.ResourceManager manager, FileContentResource.ResourceData data) : Resource<FileContentResource.ResourceManager, FileContentResource.ResourceData, FileContentResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileContentResource>.ResourceManager
  {
    private const string NAME = "File";
    private const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "File";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override FileContentResource CreateResource(ResourceData data)
    {
      throw new NotImplementedException();
    }

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : Resource<ResourceManager, ResourceData, FileContentResource>.ResourceData(Id, CreateTime, UpdateTime);
}

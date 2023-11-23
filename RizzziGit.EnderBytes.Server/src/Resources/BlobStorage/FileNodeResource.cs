using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class FileNodeResource(FileNodeResource.ResourceManager manager, FileNodeResource.ResourceData data) : Resource<FileNodeResource.ResourceManager, FileNodeResource.ResourceData, FileNodeResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceManager
  {
    private const string NAME = "FileSystem";
    private const int VERSION = 1;

    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_PARENT_ID = "ParentNode";
    private const string KEY_TYPE = "Type";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override FileNodeResource CreateResource(ResourceData data)
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
  ) : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceData(Id, CreateTime, UpdateTime);
}

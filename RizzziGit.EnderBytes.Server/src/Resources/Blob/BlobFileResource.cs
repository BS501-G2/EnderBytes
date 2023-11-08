using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobFileResource(BlobFileResource.ResourceManager manager, BlobFileResource.ResourceData data) : Resource<BlobFileResource.ResourceManager, BlobFileResource.ResourceData, BlobFileResource>(manager, data)
{
  private const byte TYPE_FILE = 0;
  private const byte TYPE_FOLDER = 1;
  private const byte TYPE_SYMBOLIC_LINK = 2;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceManager
  {
    private const string NAME = "BlobFile";
    private const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_TYPE = "Type";
    private const string KEY_PARENT_ID = "ParentId";
    private const string KEY_NAME = "Name";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobFileResource CreateResource(ResourceData data) => new(this, data);
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

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceData(Id, CreateTime, UpdateTime)
  {

  }
}

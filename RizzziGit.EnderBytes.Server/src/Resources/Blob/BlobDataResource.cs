using Microsoft.Data.Sqlite;
namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;
using Keys;

public sealed class BlobDataResource : Resource<BlobDataResource.ResourceManager, BlobDataResource.ResourceData, BlobDataResource>
{
  public const int BUFFER_SIZE = KeyGenerator.KEY_SIZE / 8;

  public BlobDataResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobDataResource>.ResourceManager
  {
    private const string NAME = "BlobData";
    private const int VERSION = 1;

    private const string KEY_VERSION_ID = "VersionId";
    private const string KEY_SIZE = "Size";
    private const string KEY_DATA = "Data";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobDataResource CreateResource(ResourceData data) => new(this, data);
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

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, BlobDataResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
  }
}

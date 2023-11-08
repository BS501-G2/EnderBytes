using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobFileVersionResource(BlobFileVersionResource.ResourceManager manager, BlobFileVersionResource.ResourceData data) : Resource<BlobFileVersionResource.ResourceManager, BlobFileVersionResource.ResourceData, BlobFileVersionResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileVersionResource>.ResourceManager
  {
    private const string NAME = "BlobVersion";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_AUTHOR_USER_ID = "AuthorUserId";
    private const string KEY_SIZE = "Size";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobFileVersionResource CreateResource(ResourceData data) => new(this, data);
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

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, BlobFileVersionResource>.ResourceData(Id, CreateTime, UpdateTime);
}

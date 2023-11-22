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

    public new BlobStorageResourceManager Main => (BlobStorageResourceManager)base.Main;

    protected override BlobFileVersionResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_AUTHOR_USER_ID],
      (long)reader[KEY_SIZE]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHOR_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_SIZE} integer not null;");
      }
    }

    public IEnumerable<BlobFileVersionResource> StreamByFile(DatabaseTransaction transaction, BlobFileResource file) => DbStream(transaction, new()
    {
      { KEY_FILE_ID, ("=", file.Id) }
    }, null, [new(KEY_CREATE_TIME, "desc")]);

    public BlobFileVersionResource Create(DatabaseTransaction transaction, BlobFileResource file, UserResource authorUser, BlobFileVersionResource? fromVersion = null)
    {
      BlobFileVersionResource version = DbInsert(transaction, new()
      {
        { KEY_FILE_ID, file.Id },
        { KEY_AUTHOR_USER_ID, authorUser.Id },
        { KEY_SIZE, fromVersion?.Size ?? 0 }
      });

      if (fromVersion != null)
      {
        Main.Maps.Clone(transaction, version, fromVersion);
      }

      return version;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long AuthorUserId,
    long Size
  ) : Resource<ResourceManager, ResourceData, BlobFileVersionResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long AuthorUserId => Data.AuthorUserId;
  public long Size => Data.Size;
}

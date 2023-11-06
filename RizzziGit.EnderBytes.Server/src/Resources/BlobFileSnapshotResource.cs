using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobFileSnapshotResource : Resource<BlobFileSnapshotResource.ResourceManager, BlobFileSnapshotResource.ResourceData, BlobFileSnapshotResource>
{
  public BlobFileSnapshotResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileSnapshotResource>.ResourceManager
  {
    private const string NAME = "BlobFileVersion";
    private const int VERSION = 1;

    private const string KEY_AUTHOR_USER_ID = "AuthorUserId";
    private const string KEY_FILE_ID = "FileId";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      Main.Files.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override BlobFileSnapshotResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHOR_USER_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
      }
    }

    public async Task<BlobFileSnapshotResource> Create(DatabaseTransaction transaction, UserResource authorUser, BlobFileResource file, CancellationToken cancellationToken)
    {
      BlobFileSnapshotResource snapshot = DbInsert(transaction, new()
      {
        { KEY_FILE_ID, file.Id },
        { KEY_AUTHOR_USER_ID, authorUser.Id }
      });

      await Main.FileData.CopyFrom(transaction, file, snapshot, null, cancellationToken);
      return snapshot;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : Resource<ResourceManager, ResourceData, BlobFileSnapshotResource>.ResourceData(Id, CreateTime, UpdateTime);
}



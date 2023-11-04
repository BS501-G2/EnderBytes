using Microsoft.Data.Sqlite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

public sealed class BlobFileVersionResource : Resource<BlobFileVersionResource.ResourceManager, BlobFileVersionResource.ResourceData, BlobFileVersionResource>
{
  public BlobFileVersionResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileVersionResource>.ResourceManager
  {
    private const string NAME = "BlobFileVersion";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_ORIGINAL_VERSION_ID = "VersionId";
    private const string KEY_AUTHOR_USER_ID = "AuthorUserId";

    public ResourceManager(MainResourceManager main, Database.Database database) : base(main, database, NAME, VERSION)
    {
      main.BlobFiles.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override BlobFileVersionResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      reader[KEY_ORIGINAL_VERSION_ID] is DBNull ? null : (long)reader[KEY_ORIGINAL_VERSION_ID],
      (long)reader[KEY_AUTHOR_USER_ID]
    );

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ORIGINAL_VERSION_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHOR_USER_ID} integer not null;");
      }
    }

    public BlobFileVersionResource Create(
      DatabaseTransaction transaction,
      BlobFileResource file,
      BlobFileVersionResource? originalVersion,
      UserResource authorUser
    ) => DbInsert(transaction, new()
    {
      { KEY_FILE_ID, file.Id },
      { KEY_ORIGINAL_VERSION_ID, originalVersion?.Id },
      { KEY_AUTHOR_USER_ID, authorUser.Id }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long? OriginalVersionId,
    long AuthorUserId
  ) : Resource<ResourceManager, ResourceData, BlobFileVersionResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long? OriginalVersionId => Data.OriginalVersionId;
  public long AuthorUserId => Data.AuthorUserId;
}

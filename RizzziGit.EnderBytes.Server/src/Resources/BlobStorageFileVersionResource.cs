using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using System.Text.Json.Serialization;
using Database;

public sealed class BlobStorageFileVersionResource(BlobStorageFileVersionResource.ResourceManager manager, BlobStorageFileVersionResource.ResourceData data) : Resource<BlobStorageFileVersionResource.ResourceManager, BlobStorageFileVersionResource.ResourceData, BlobStorageFileVersionResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobStorageFileVersionResource>.ResourceManager
  {
    private const string NAME = "BlobStorageFileVersion";
    private const int VERSION = 1;

    private const string KEY_BASE_VERSION_ID = "BaseVersionId";
    private const string KEY_FILE_ID = "FileId";
    private const string KEY_AUTHOR_USER_ID = "AuthorUserId";
    private const string KEY_SIZE = "Side";
    private const string KEY_BUFFER_SIZE = "BufferSize";

    private const string INDEX_BASE_VERSION_ID = $"Index_{NAME}_{KEY_BASE_VERSION_ID}_{KEY_FILE_ID}";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.BlobStorageFiles.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      reader[KEY_BASE_VERSION_ID] is not DBNull ? (long)reader[KEY_BASE_VERSION_ID] : null,
      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_AUTHOR_USER_ID],
      (long)reader[KEY_SIZE],
      (long)reader[KEY_BUFFER_SIZE]
    );

    protected override BlobStorageFileVersionResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BASE_VERSION_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHOR_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_SIZE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BUFFER_SIZE} integer not null;");

        transaction.ExecuteNonQuery($"create index {INDEX_BASE_VERSION_ID} on {NAME}({KEY_BASE_VERSION_ID},{KEY_FILE_ID})");
      }
    }

    public BlobStorageFileVersionResource Create(
      DatabaseTransaction transaction,
      BlobStorageFileResource file,
      BlobStorageFileVersionResource? baseVerion,
      UserResource authorUser
    ) => DbInsert(transaction, new()
    {
      { KEY_BASE_VERSION_ID, baseVerion?.Id },
      { KEY_FILE_ID, file.Id },
      { KEY_AUTHOR_USER_ID, authorUser.Id },
      { KEY_SIZE, 0 }
    });

    public Task Update(
      DatabaseTransaction transaction,
      BlobStorageFileVersionResource fileVersion,
      long size,
      CancellationToken cancellationToken
    ) => DbUpdate(transaction, new()
    {
      { KEY_SIZE, size },
    }, new()
    {
      { KEY_ID, ("=", fileVersion.Id, null) }
    }, cancellationToken);
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? BaseVersionId,
    long FileId,
    long AuthorUserId,
    long Size,
    long BufferSize
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileVersionResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_BASE_VERSION_ID = "baseVersionId";
    public const string KEY_FILE_ID = "fileId";
    public const string KEY_AUTHOR_USER_ID = "authorUserId";
    public const string KEY_SIZE = "size";
    public const string KEY_BUFFER_SIZE = "bufferSize";

    [JsonPropertyName(KEY_BASE_VERSION_ID)] public long? BaseVersionId = BaseVersionId;
    [JsonPropertyName(KEY_FILE_ID)] public long FileId = FileId;
    [JsonPropertyName(KEY_AUTHOR_USER_ID)] public long AuthorUserId = AuthorUserId;
    [JsonPropertyName(KEY_SIZE)] public long Size = Size;
    [JsonPropertyName(KEY_BUFFER_SIZE)] public long BufferSize = BufferSize;
  }

  public long? BaseVersionId => Data.BaseVersionId;
  public long FileId => Data.FileId;
  public long AuthorUserId => Data.AuthorUserId;
  public long Size => Data.Size;
  public long BufferSize => Data.BufferSize;
}

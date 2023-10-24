using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobStorageFileDataResource(BlobStorageFileDataResource.ResourceManager manager, BlobStorageFileDataResource.ResourceData data) : Resource<BlobStorageFileDataResource.ResourceManager, BlobStorageFileDataResource.ResourceData, BlobStorageFileDataResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobStorageFileDataResource>.ResourceManager
  {
    private const string NAME = "BlobStorageFileData";
    private const int VERSION = 1;

    private const string KEY_KEY_ID = "KeyId";
    private const string KEY_FILE_VERSION_ID = "FileId";
    private const string KEY_FILE_INDEX = "FileIndex";
    private const string KEY_BLOB_OFFSET = "BlobOffset";
    private const string KEY_BLOB_LENGTH = "BlobLength";
    private const string KEY_DATA_LENGTH = "DataLength";

    private const string INDEX_FILE_VERSION_ID = $"Index_{NAME}_{KEY_FILE_VERSION_ID}_{KEY_FILE_INDEX}";
    private const string INDEX_BLOB_OFFSET = $"Index_{NAME}_{KEY_BLOB_OFFSET}";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.BlobStorageFileVersions.OnResourceDelete((transaction, resource, cancelationToken) => DbDelete(transaction, new()
      {
        { KEY_FILE_VERSION_ID, ("=", resource.Id, null) }
      }, cancelationToken));
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (long)reader[KEY_KEY_ID],
      (long)reader[KEY_FILE_VERSION_ID],
      (long)reader[KEY_FILE_INDEX],
      (long)reader[KEY_BLOB_OFFSET],
      (long)reader[KEY_BLOB_LENGTH],
      (long)reader[KEY_DATA_LENGTH]
    );

    protected override BlobStorageFileDataResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_KEY_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_VERSION_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_INDEX} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BLOB_OFFSET} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BLOB_LENGTH} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_DATA_LENGTH} integer not null;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_FILE_VERSION_ID} on {NAME}({KEY_FILE_VERSION_ID},{KEY_FILE_INDEX});");
        transaction.ExecuteNonQuery($"create unique index {INDEX_BLOB_OFFSET} on {NAME}({KEY_BLOB_OFFSET});");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long KeyId,
    long FileVersionId,
    long FileIndex,
    long BlobOffset,
    long BlobLength,
    long DataLength
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileDataResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_KEY_ID = "keyId";
    public const string KEY_FILE_VERSION_ID = "fileVersionId";
    public const string KEY_FILE_INDEX = "fileIndex";
    public const string KEY_BLOB_OFFSET = "blobOffset";
    public const string KEY_BLOB_LENGTH = "blobLength";
    public const string KEY_DATA_LENGTH = "dataLength";

    [JsonPropertyName(KEY_KEY_ID)] public long KeyId = KeyId;
    [JsonPropertyName(KEY_FILE_VERSION_ID)] public long FileVersionId = FileVersionId;
    [JsonPropertyName(KEY_FILE_INDEX)] public long FileIndex = FileIndex;
    [JsonPropertyName(KEY_BLOB_OFFSET)] public long BlobOffset = BlobOffset;
    [JsonPropertyName(KEY_BLOB_LENGTH)] public long BlobLength = BlobLength;
    [JsonPropertyName(KEY_DATA_LENGTH)] public long DataLength = DataLength;
  }

  public long KeyId => Data.KeyId;
  public long FileVersionId => Data.FileVersionId;
  public long FileIndex => Data.FileIndex;
  public long BlobOffset => Data.BlobOffset;
  public long BlobLength => Data.BlobLength;
  public long DataLength => Data.DataLength;
}

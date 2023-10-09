using System.Data.SQLite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobStorageFileBlobResource(BlobStorageFileBlobResource.ResourceManager manager, BlobStorageFileBlobResource.ResourceData data) : Resource<BlobStorageFileBlobResource.ResourceManager, BlobStorageFileBlobResource.ResourceData, BlobStorageFileBlobResource>(manager, data)
{
  public const string NAME = "VSBlob";
  public const int VERSION = 1;

  private const string KEY_FILE_VERSION_ID = "FileVersionID";
  private const string KEY_BLOB_BUFFER_INDEX = "BlobBufferIndex";
  private const string KEY_ENCRYPTION_KEY_INDEX = "EncryptionKeyIndex";
  private const string KEY_ENCRYPTION_IV = "EncryptionIV";
  private const string KEY_BLOB_BEGIN = "BlobBegin";
  private const string KEY_BLOB_END = "BlobEnd";

  private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_FILE_VERSION_ID}_{KEY_BLOB_BUFFER_INDEX}";

  public new sealed class ResourceData(
    ulong id,
    long createTime,
    long updateTime,
    ulong? fileVersionId,
    long? blobBufferIndex,
    long encryptionKeyIndex,
    byte[] encryptionIv,
    long blobBegin,
    long blobEnd
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileBlobResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong? FileVersionID = fileVersionId;
    public long? BlobBufferIndex = blobBufferIndex;
    public long EncryptionKeyIndex = encryptionKeyIndex;
    public byte[] EncryptionIV = encryptionIv;
    public long BlobBegin = blobBegin;
    public long BlobEnd = blobEnd;
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, BlobStorageFileBlobResource>.ResourceManager(main, VERSION, NAME)
  {
    private readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (ulong)(long)reader[KEY_FILE_VERSION_ID],
      (long)reader[KEY_BLOB_BUFFER_INDEX],
      (long)reader[KEY_ENCRYPTION_KEY_INDEX],
      (byte[])reader[KEY_ENCRYPTION_IV],
      (long)reader[KEY_BLOB_BEGIN],
      (long)reader[KEY_BLOB_END]
    );

    protected override BlobStorageFileBlobResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_FILE_VERSION_ID} integer;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BLOB_BUFFER_INDEX} integer;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_ENCRYPTION_KEY_INDEX} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_ENCRYPTION_IV} blob not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BLOB_BEGIN} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BLOB_END} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_FILE_VERSION_ID},{KEY_BLOB_BUFFER_INDEX})", cancellationToken);
      }
    }

    public Task<ResourceStream> StreamByIndex(
      SQLiteConnection connection,
      (BlobStorageFileVersionResource fileVersion, long index)? search,
      (int? offset, int count)? limit,
      (string column, string orderBy)? order,
      CancellationToken cancellationToken
    ) => DbSelect(connection, new()
    {
      { KEY_FILE_VERSION_ID, ("=", search?.fileVersion.ID, null) },
      { KEY_BLOB_BUFFER_INDEX, ("=", search?.index, null) }
    }, limit, order, cancellationToken);

    public Task<BlobStorageFileBlobResource?> GetByIndex(
      SQLiteConnection connection,
      (BlobStorageFileVersionResource fileVersion, long index)? search,
      int? offset,
      (string column, string orderBy)? order,
      CancellationToken cancellationToken
    ) => DbSelectOne(
      connection, new() {
      { KEY_FILE_VERSION_ID, ("=", search?.fileVersion.ID, null) },
      { KEY_BLOB_BUFFER_INDEX, ("=", search?.index, null) }
      }, offset, order, cancellationToken
    );

    public Task<BlobStorageFileBlobResource> Create(SQLiteConnection connection, BlobStorageFileVersionResource fileVersion, BlobStorageKeyResource storageKey, long index, long bufferBegin, long bufferEnd, byte[] iv, CancellationToken cancellationToken) => DbInsert(connection, new()
    {
      { KEY_FILE_VERSION_ID, fileVersion.ID },
      { KEY_BLOB_BUFFER_INDEX, index },
      { KEY_ENCRYPTION_KEY_INDEX, storageKey.Index },
      { KEY_ENCRYPTION_IV, iv },
      { KEY_BLOB_BEGIN, bufferBegin },
      { KEY_BLOB_BEGIN, bufferEnd }
    }, cancellationToken);
  }

  public ulong? FileVersionID => Data.FileVersionID;
  public long? BlobBufferIndex => Data.BlobBufferIndex;
  public long EncryptionKeyIndex => Data.EncryptionKeyIndex;
  public byte[] EncryptionIV => Data.EncryptionIV;
  public long BlobBegin => Data.BlobBegin;
  public long BlobEnd => Data.BlobEnd;
}

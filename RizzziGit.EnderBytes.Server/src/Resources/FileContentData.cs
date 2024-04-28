using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;
using Services;

using ResourceManager = ResourceManager<FileContentDataManager, FileContentDataManager.Resource>;

public sealed partial class FileContentDataManager : ResourceManager
{
  public const int BUFFER_SIZE = 1024 * 1024;

  public const string NAME = "FileBlob";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_METADATA_ID = "FileMetadataId";
  public const string COLUMN_BLOB_ID = "BlobId";
  public const string COLUMN_BLOB_START = "BlobStart";
  public const string COLUMN_BLOB_END = "BlobEnd";

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    long MetadataId,
    long BlobId,
    long BlobStart,
    long BlobEnd
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public FileContentDataManager(ResourceService service) : base(service, NAME, VERSION)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_METADATA_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_BLOB_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_BLOB_START)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_BLOB_END))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_METADATA_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB_START} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB_END} bigint not null;");
    }
  }

  public Task<long> GetSize(ResourceService.Transaction transaction, FileManager.Resource file) => SqlScalar<long>(transaction, $"select sum({COLUMN_BLOB_END} - {COLUMN_BLOB_START}) from {NAME} where {COLUMN_FILE_ID} = {file.Id} limit 1;");
}

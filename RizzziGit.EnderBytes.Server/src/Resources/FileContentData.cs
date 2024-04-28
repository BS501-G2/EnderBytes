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
  public const string COLUMN_FILE_CONTENT_ID = "FileContentId";
  public const string COLUMN_BLOB_ID = "BlobId";
  public const string COLUMN_BLOB_SIZE = "BlobSize";

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    long MetadataId,
    long BlobId,
    long BlobSize
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public FileContentDataManager(ResourceService service) : base(service, NAME, VERSION)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_CONTENT_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_BLOB_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_BLOB_SIZE))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_CONTENT_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB_SIZE} bigint not null;");
    }
  }

  public Task<long> GetSize(ResourceService.Transaction transaction, FileManager.Resource file, FileContentManager.Resource fileContent) => SqlScalar<long>(transaction, $"select sum({COLUMN_BLOB_SIZE}) from {NAME} where {COLUMN_FILE_ID} = {{0}} and {COLUMN_FILE_CONTENT_ID} = {{1}} limit 1;", [file.Id, fileContent.Id]);

  public async Task<CompositeBuffer> Read(ResourceService.Transaction transaction, FileManager.Resource file, FileContentManager.Resource fileContent, long? start = null, long? end = null)
  {
    long size = await GetSize(transaction, file, fileContent);
    long length = (end ?? 0) - (start ?? size);

    long requestStart = start ?? 0;
    long requestEnd = end ?? size;

    long limitOffset = requestStart / BUFFER_SIZE;
    long limitCount = (length + BUFFER_SIZE - 1) / BUFFER_SIZE;

    CompositeBuffer bytes = [];

    foreach (Resource resource in await Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id)
    ), new(limitCount, limitOffset), null))
    {
      bytes.Append((await GetManager<FileContentDataBlobManager>().GetById(transaction, resource.BlobId))!.Blob);
    }

    bytes.SpliceStart((limitOffset * BUFFER_SIZE) - requestStart);
    bytes.SpliceEnd(requestEnd - requestStart);

    return bytes;
  }

  public async Task Write(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, FileContentManager.Resource fileContent, CompositeBuffer blob, long blobOffset = 0)
  {
    blob = blob.Clone();

    long startIndexOffset = blobOffset % BUFFER_SIZE;
    long startIndex = blobOffset / BUFFER_SIZE;

    long length = long.Max(await GetSize(transaction, file, fileContent), blobOffset + blob.Length);

    long count = (length + BUFFER_SIZE - 1) / BUFFER_SIZE;

    for (long index = 0; index < count; index++)
    {
      Resource? fileContentData = await SelectOne(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id)
      ), index);

      if (fileContentData is null)
      {
        CompositeBuffer buffer = CompositeBuffer.Allocate(BUFFER_SIZE);

        if (index == startIndex)
        {
          buffer.Write(startIndexOffset, blob.SpliceStart(long.Min(BUFFER_SIZE, blob.Length) - startIndexOffset));
        }
        else if (index > startIndex)
        {
          buffer.Write(0, blob.SpliceStart(long.Min(BUFFER_SIZE, blob.Length)));
        }

        long fileContentDataBlobId = await GetManager<FileContentDataBlobManager>().Write(transaction, file, fileKey, buffer.ToByteArray());

        await Insert(transaction, new(
          (COLUMN_FILE_ID, file.Id),
          (COLUMN_FILE_CONTENT_ID, fileContent.Id),
          (COLUMN_BLOB_ID, fileContentDataBlobId),
          (COLUMN_BLOB_SIZE, long.Min(length - (index * BUFFER_SIZE), BUFFER_SIZE))
        ));
      }
      else
      {
        CompositeBuffer buffer = await GetManager<FileContentDataBlobManager>().Read(transaction, file, fileKey, fileContentData.BlobId);

        if (index == startIndex)
        {
          buffer.Write(startIndexOffset, blob.SpliceStart(long.Min(BUFFER_SIZE, blob.Length) - startIndexOffset));
        }
        else if (index > startIndex)
        {
          buffer.Write(0, blob.SpliceStart(long.Min(BUFFER_SIZE, blob.Length)));
        }

        await Update(transaction, fileContentData, new(
          (COLUMN_BLOB_SIZE, long.Min(length - (index * BUFFER_SIZE), BUFFER_SIZE))
        ));
      }
    }
  }
}

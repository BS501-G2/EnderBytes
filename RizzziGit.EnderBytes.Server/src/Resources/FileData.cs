using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileDataManager, FileDataManager.Resource>;
using RizzziGit.Commons.Memory;

public sealed class FileDataManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    long FileContentId,
    long FileContentVersionId,
    long BlobId,
    long Index,
    long Size
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const int BUFFER_SIZE = 1024 * 1024;

  public const string NAME = "FileData";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_FILE_CONTENT_ID = "FileContentId";
  public const string COLUMN_FILE_CONTENT_VERSION_ID = "FileContentVersionId";
  public const string COLUMN_FILE_BLOB_ID = "BlobId";
  public const string COLUMN_INDEX = "BlobIndex";
  public const string COLUMN_SIZE = "Size";

  public FileDataManager(ResourceService service) : base(service, NAME, VERSION)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_CONTENT_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_CONTENT_VERSION_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_BLOB_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_INDEX)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_SIZE))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_CONTENT_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_CONTENT_VERSION_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_BLOB_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INDEX} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_SIZE} bigint not null;");
    }
  }

  public async Task<long> GetExclusiveMaxIndex(ResourceService.Transaction transaction, FileManager.Resource file, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion)
  {
    return (await SqlScalar<long>(transaction, $"select max({COLUMN_INDEX}) from {NAME} where {COLUMN_FILE_ID} = {{0}} and {COLUMN_FILE_CONTENT_ID} = {{1}} and {COLUMN_FILE_CONTENT_VERSION_ID} = {{2}};", file.Id, fileContent.Id, fileContentVersion.Id)) + 1;
  }

  public async Task<long> GetSize(ResourceService.Transaction transaction, FileManager.Resource file, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion)
  {
    long maxIndex = await GetExclusiveMaxIndex(transaction, file, fileContent, fileContentVersion);

    Resource fileData = (await SelectFirst(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_VERSION_ID, "=", fileContentVersion.Id),
      new WhereClause.CompareColumn(COLUMN_INDEX, "=", maxIndex - 1)
    )))!;

    return (fileData.Index * BUFFER_SIZE) + fileData.Size;
  }

  public async Task<CompositeBuffer> Read(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion, long position, long length)
  {
  }

  public async Task Write(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion, long position, CompositeBuffer bytes)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(position, 0, nameof(position));

    bytes = bytes.Clone();
    bytes.CopyOnWrite = true;

    long startIndex = position / BUFFER_SIZE;
    long endIndex = (position + bytes.Length + BUFFER_SIZE - 1) / BUFFER_SIZE;

    for (long index = startIndex; index < endIndex; index++)
    {
      Resource? fileData = await SelectFirst(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_VERSION_ID, "=", fileContentVersion.Id),
        new WhereClause.CompareColumn(COLUMN_INDEX, "=", index)
      ));

      CompositeBuffer buffer = fileData == null
        ? CompositeBuffer.Allocate(BUFFER_SIZE)
        : await GetManager<FileBlobManager>().Retrieve(transaction, fileKey, fileData.BlobId);

      long bufferStart = index == startIndex ? startIndex : 0;

      CompositeBuffer toWrite = bytes.SpliceStart(long.Min(buffer.Length - bufferStart, bytes.Length));
      buffer.Write(bufferStart, toWrite);

      long newBlobId = await GetManager<FileBlobManager>().Store(transaction, fileKey, buffer.ToByteArray());

      if (fileData != null)
      {
        await Update(transaction, fileData, new(
          (COLUMN_SIZE, long.Max(bufferStart + toWrite.Length, fileData.Size)),
          (COLUMN_FILE_BLOB_ID, newBlobId)
        ));
      }
      else
      {
        await Insert(transaction, new(
          (COLUMN_FILE_ID, file.Id),
          (COLUMN_FILE_CONTENT_ID, fileContent.Id),
          (COLUMN_FILE_CONTENT_VERSION_ID, fileContentVersion.Id),
          (COLUMN_INDEX, index),
          (COLUMN_SIZE, bufferStart + toWrite.Length),
          (COLUMN_FILE_BLOB_ID, newBlobId)
        ));
      }
    }
  }
}

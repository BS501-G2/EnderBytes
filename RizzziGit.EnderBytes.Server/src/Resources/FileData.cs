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

  public async Task<long> GetSize(ResourceService.Transaction transaction, FileManager.Resource file, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion)
  {
    return (long)(await SqlScalar<double>(transaction, $"select sum({COLUMN_SIZE}) from {NAME} where {COLUMN_FILE_ID} = {{0}} and {COLUMN_FILE_CONTENT_ID} = {{1}} and {COLUMN_FILE_CONTENT_VERSION_ID} = {{2}};", file.Id, fileContent.Id, fileContentVersion.Id))!;
  }

  public async Task<CompositeBuffer> Read(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion, long position, long length)
  {
    long maxIndex = (long)(await SqlScalar(transaction, $"select max({COLUMN_INDEX}) from {NAME} where {COLUMN_FILE_ID} = {{0}} and {COLUMN_FILE_CONTENT_ID} = {{1}} and {COLUMN_FILE_CONTENT_VERSION_ID} = {{2}};", file.Id, fileContent.Id, fileContentVersion.Id))!;
    long size = await GetSize(transaction, file, fileContent, fileContentVersion);

    ArgumentOutOfRangeException.ThrowIfLessThan(position, 0, nameof(position));
    ArgumentOutOfRangeException.ThrowIfGreaterThan(position, size, nameof(position));
    ArgumentOutOfRangeException.ThrowIfLessThan(length, 0, nameof(length));
    ArgumentOutOfRangeException.ThrowIfGreaterThan(position + length, size, nameof(length));

    long startIndex = position / BUFFER_SIZE;
    long endIndex = startIndex + ((length + BUFFER_SIZE - 1) / BUFFER_SIZE);

    CompositeBuffer bytes = [];
    long toRead = length;
    for (long index = startIndex; index <= endIndex; index++)
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

      buffer = buffer.Splice(index == startIndex ? position % BUFFER_SIZE : 0, long.Min(toRead, fileData?.Size ?? BUFFER_SIZE));
      bytes.Append(buffer);
      toRead -= buffer.Length;
    }

    return bytes;
  }

  public async Task Write(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, FileContentManager.Resource fileContent, FileContentVersionManager.Resource fileContentVersion, long position, CompositeBuffer bytes)
  {
    long startIndex = position / BUFFER_SIZE;
    long endIndex = startIndex + ((bytes.Length + BUFFER_SIZE - 1) / BUFFER_SIZE);

    ArgumentOutOfRangeException.ThrowIfLessThan(startIndex, 0, nameof(position));
    ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, endIndex, nameof(position));

    for (long index = startIndex; index <= endIndex; index++)
    {
      Resource? fileData = await SelectFirst(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_VERSION_ID, "=", fileContentVersion.Id),
        new WhereClause.CompareColumn(COLUMN_INDEX, "=", index)
      ));

      CompositeBuffer buffer = fileData == null
        ? CompositeBuffer.Allocate(BUFFER_SIZE)
        : await GetManager<FileBlobManager>().Retrieve(transaction, fileKey, index);

      long bufferOffset = index == startIndex ? position % BUFFER_SIZE : 0;
      CompositeBuffer toWrite = bytes.SpliceStart(long.Max(buffer.Length - bufferOffset, bytes.Length));

      buffer.Write(bufferOffset, toWrite);

      if (fileData == null)
      {
        await InsertAndGet(transaction, new(
          (COLUMN_FILE_ID, file.Id),
          (COLUMN_FILE_CONTENT_ID, fileContent.Id),
          (COLUMN_FILE_CONTENT_VERSION_ID, fileContentVersion.Id),
          (COLUMN_FILE_BLOB_ID, await GetManager<FileBlobManager>().Store(transaction, fileKey, buffer.ToByteArray())),
          (COLUMN_INDEX, index),
          (COLUMN_SIZE, toWrite.Length)
        ));
      }
      else
      {
        await Update(transaction, fileData, new(
          (COLUMN_FILE_BLOB_ID, await GetManager<FileBlobManager>().Store(transaction, fileKey, buffer.ToByteArray())),
          (COLUMN_INDEX, index),
          (COLUMN_SIZE, long.Max(toWrite.Length, fileData.Size))
        ));
      }
    }
  }
}

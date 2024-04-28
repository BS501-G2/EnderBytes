using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileContentDataBlobManager, FileContentDataBlobManager.Resource>;

public sealed class FileContentDataBlobManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    byte[] Blob
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "FileBlob";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_BLOB = "Blob";

  public FileContentDataBlobManager(ResourceService service) : base(service, NAME, VERSION)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_BLOB))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BLOB} blob not null;");
    }
  }

  public Task<long> Write(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, byte[] buffer) => Insert(transaction, new(
    (COLUMN_FILE_ID, file.Id),
    (COLUMN_BLOB, fileKey.Encrypt(buffer))
  ));

  public async Task<CompositeBuffer> Read(ResourceService.Transaction transaction, FileManager.Resource file, KeyService.AesPair fileKey, long id)
  {
    Resource? fileContentDataBlob = await GetById(transaction, id);

    if (fileContentDataBlob == null || file.Id != fileContentDataBlob.FileId)
    {
      return [];
    }

    return fileKey.Decrypt(fileContentDataBlob.Blob);
  }
}

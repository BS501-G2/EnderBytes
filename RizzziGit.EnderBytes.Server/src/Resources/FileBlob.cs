using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileBlobManager, FileBlobManager.Resource>;

public sealed class FileBlobManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    byte[] Buffer
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "FileBlob";
  public const int VERSION = 1;

  public const string COLUMN_BUFFER = "Buffer";

  public FileBlobManager(ResourceService service, string name, int version) : base(service, name, version)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetBytes(reader.GetOrdinal(COLUMN_BUFFER))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BUFFER} blob not null;");
    }
  }

  public async Task<long> Store(ResourceService.Transaction transaction, KeyService.AesPair fileKey, byte[] buffer)
  {
    return await Insert(transaction, new(
      (COLUMN_BUFFER, fileKey.Encrypt(buffer))
    ));
  }

  public async Task<byte[]> Retrieve(ResourceService.Transaction transaction, KeyService.AesPair fileKey, long blobId)
  {
    Resource fileBlob = await GetByRequiredId(transaction, blobId);

    return fileKey.Decrypt(fileBlob.Buffer);
  }
}

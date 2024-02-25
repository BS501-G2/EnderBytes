using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileBufferResource(FileBufferResource.ResourceManager manager, FileBufferResource.ResourceData data) : Resource<FileBufferResource.ResourceManager, FileBufferResource.ResourceData, FileBufferResource>(manager, data)
{
  private const string NAME = "FileBuffer";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileBufferResource>.ResourceManager
  {
    private const string COLUMN_STORAGE_ID = "StorageId";
    private const string COLUMN_FILE_ID = "FileId";
    private const string COLUMN_BUFFER = "Buffer";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.FileBufferMaps.ResourceDeleted += (transaction, fileBufferMap, cancellationToken) =>
      {
        if (fileBufferMap.FileBufferId == null)
        {
          return;
        }

        if (!TryGetById(transaction, (long)fileBufferMap.FileBufferId, out FileBufferResource? fileBuffer, cancellationToken))
        {
          return;
        }

        if (Service.FileBufferMaps.GetReferenceCount(transaction, fileBuffer, cancellationToken) == 0)
        {
          Delete(transaction, fileBuffer, cancellationToken);
        }
      };
    }

    protected override FileBufferResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_STORAGE_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_BUFFER))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_STORAGE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BUFFER} blob not null;");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long StorageId,
    long FileId,
    byte[] Buffer
  ) : Resource<ResourceManager, ResourceData, FileBufferResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long StorageId => Data.StorageId;
  public long FileId => Data.FileId;
  public byte[] Buffer => Data.Buffer;
}

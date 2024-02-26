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
    private const string COLUMN_BUFFER = "Buffer";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      // Service.FileBufferMaps.ResourceUpdated += (transaction, fileBufferMap, cancellationToken) => check(transaction, fileBufferMap, cancellationToken);
      // Service.FileBufferMaps.ResourceDeleted += (transaction, fileBufferMap, cancellationToken) => check(transaction, fileBufferMap.FileBufferId, cancellationToken);

      // void check(ResourceService.Transaction transaction, long? fileBufferId, CancellationToken cancellationToken)
      // {
      // }
    }

    protected override FileBufferResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetBytes(reader.GetOrdinal(COLUMN_BUFFER))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BUFFER} mediumblob not null;");
      }
    }

    public bool Update(ResourceService.Transaction transaction, FileBufferResource fileBuffer, byte[] buffer, CancellationToken cancellationToken = default)
    {
      return Update(transaction, fileBuffer, new SetClause(
        (COLUMN_BUFFER, buffer)
      ), cancellationToken);
    }

    public long Create(ResourceService.Transaction transaction, byte[] buffer, CancellationToken cancellationToken = default)
    {
      return Insert(transaction, new(
        (COLUMN_BUFFER, buffer)
      ), cancellationToken);
    }

    public long Delete(ResourceService.Transaction transaction, long id, CancellationToken cancellationToken  = default)
    {
      return Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", id), cancellationToken);
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    byte[] Buffer
  ) : Resource<ResourceManager, ResourceData, FileBufferResource>.ResourceData(Id, CreateTime, UpdateTime);

  public byte[] Buffer => Data.Buffer;
}

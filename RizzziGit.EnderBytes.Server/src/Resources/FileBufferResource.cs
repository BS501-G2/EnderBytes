using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed record FileBufferResource(FileBufferResource.ResourceManager manager,
  long Id,
  long CreateTime,
  long UpdateTime,

  byte[] Buffer
) : Resource<FileBufferResource.ResourceManager, FileBufferResource>(manager, Id, CreateTime, UpdateTime)
{
  public const string NAME = "FileBuffer";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, FileBufferResource>.ResourceManager
  {
    public const string COLUMN_BUFFER = "Buffer";
    public const string COLUMN_FILE_ID = "FileId";

    public const string INDEX_FILE_ID = $"Index_{NAME}_{COLUMN_FILE_ID}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.GetManager<FileResource.ResourceManager>().ResourceDeleted += (transaction, resource, cancellationToken) =>
      {
        if (resource.Type == FileType.File)
        {
          SqlNonQuery(transaction, $"delete from {NAME} where {COLUMN_FILE_ID} = {{0}}", [resource.Id]);
        }
      };
    }

    protected override FileBufferResource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
      this, id, createTime, updateTime,

      reader.GetBytes(reader.GetOrdinal(COLUMN_BUFFER))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BUFFER} mediumblob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"create index {INDEX_FILE_ID} on {NAME}({COLUMN_FILE_ID});");
      }
    }

    public bool Update(ResourceService.Transaction transaction, FileBufferResource fileBuffer, byte[] buffer, CancellationToken cancellationToken = default)
    {
      return Update(transaction, fileBuffer, new SetClause(
        (COLUMN_BUFFER, buffer)
      ), cancellationToken);
    }

    public long Create(ResourceService.Transaction transaction, FileResource file, byte[] buffer, CancellationToken cancellationToken = default)
    {
      if (file.Type != FileType.File)
      {
        throw new ArgumentException("Not a file.", nameof(file));
      }

      return Insert(transaction, new(
        (COLUMN_BUFFER, buffer),
        (COLUMN_FILE_ID, file.Id)
      ), cancellationToken);
    }

    public long Delete(ResourceService.Transaction transaction, long id, CancellationToken cancellationToken = default)
    {
      return Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", id), cancellationToken);
    }

    public void DeleteIfNotReferenced(ResourceService.Transaction transaction, long? fileBufferId, CancellationToken cancellationToken = default)
    {
      if (fileBufferId == null)
      {
        return;
      }

      string otherTable = FileBufferMapResource.NAME;
      string otherColumn = FileBufferMapResource.ResourceManager.COLUMN_FILE_BUFFER_ID;

      SqlNonQuery(transaction, $"delete from {NAME} where ({COLUMN_ID} = {{0}}) and ({COLUMN_ID} not in (select {otherColumn} from {otherTable} where {otherColumn} is not null));", fileBufferId);
    }
  }
}

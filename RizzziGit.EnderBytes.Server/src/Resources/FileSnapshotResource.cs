using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed record FileSnapshotResource(FileSnapshotResource.ResourceManager manager,
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long? BaseSnapshotId,
    long? AuthorFileAccessId,
    long? AuthorId
) : Resource<FileSnapshotResource.ResourceManager, FileSnapshotResource>(manager, Id, CreateTime, UpdateTime)
{
  public const string NAME = "FileSnapshot";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, FileSnapshotResource>.ResourceManager
  {
    public const string COLUMN_FILE_ID = "FileId";
    public const string COLUMN_BASE_SNAPSHOT_ID = "BaseSnapshotId";
    public const string COLUMN_AUTHOR_FILE_ACCESS_ID = "TokenId";
    public const string COLUMN_AUTHOR_ID = "AuthorId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.GetManager<FileResource.ResourceManager>().ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), cancellationToken);
    }

    protected override FileSnapshotResource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
      this, id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_BASE_SNAPSHOT_ID)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_AUTHOR_FILE_ACCESS_ID)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_AUTHOR_ID))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BASE_SNAPSHOT_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_FILE_ACCESS_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_ID} bigint null;");
      }
    }

    public FileSnapshotResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource? baseFileSnapshot, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null, CancellationToken cancellationToken = default)
    {
      (_, FileAccessResource? fileAccess) = Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken);

      var fileSnapshot = InsertAndGet(transaction, new(
        (COLUMN_FILE_ID, file.Id),
        (COLUMN_BASE_SNAPSHOT_ID, baseFileSnapshot?.Id),
        (COLUMN_AUTHOR_FILE_ACCESS_ID, fileAccess?.Id),
        (COLUMN_AUTHOR_ID, userAuthenticationToken?.UserId)
      ), cancellationToken);

      if (baseFileSnapshot != null)
      {
        Service.GetManager<FileBufferMapResource.ResourceManager>().Initialize(transaction, storage, file, fileSnapshot, cancellationToken);
      }

      return fileSnapshot;
    }

    public IEnumerable<FileSnapshotResource> List(ResourceService.Transaction transaction, StorageResource storage, FileResource file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null, LimitClause? limit = null, OrderByClause? orderBy = null, CancellationToken cancellationToken = default)
    {
      file.ThrowIfDoesNotBelongTo(storage);
      _ = Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

      return Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), limit, orderBy, cancellationToken);
    }
  }
}

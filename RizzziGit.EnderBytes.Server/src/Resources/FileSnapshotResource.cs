using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileSnapshotResource(FileSnapshotResource.ResourceManager manager, FileSnapshotResource.ResourceData data) : Resource<FileSnapshotResource.ResourceManager, FileSnapshotResource.ResourceData, FileSnapshotResource>(manager, data)
{
  public const string NAME = "FileSnapshot";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceManager
  {
    public const string COLUMN_FILE_ID = "FileId";
    public const string COLUMN_BASE_SNAPSHOT_ID = "BaseSnapshotId";
    public const string COLUMN_AUTHOR_FILE_ACCESS_ID = "TokenId";
    public const string COLUMN_AUTHOR_ID = "AuthorId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.GetManager<FileResource.ResourceManager>().ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), cancellationToken);
    }

    protected override FileSnapshotResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

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
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();
        }

        if (baseFileSnapshot == null)
        {
          return create();
        }

        lock (baseFileSnapshot)
        {
          baseFileSnapshot.ThrowIfInvalid();

          FileSnapshotResource fileSnapshot = create();

          Service.GetManager<FileBufferMapResource.ResourceManager>().Initialize(transaction, storage, file, fileSnapshot, cancellationToken);
          return fileSnapshot;
        }

        FileSnapshotResource create()
        {
          (_, FileAccessResource? fileAccess) = Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

          return InsertAndGet(transaction, new(
            (COLUMN_FILE_ID, file.Id),
            (COLUMN_BASE_SNAPSHOT_ID, baseFileSnapshot?.Id),
            (COLUMN_AUTHOR_FILE_ACCESS_ID, fileAccess?.Id),
            (COLUMN_AUTHOR_ID, userAuthenticationToken?.UserId)
          ), cancellationToken);
        }
      }
    }

    public IEnumerable<FileSnapshotResource> List(ResourceService.Transaction transaction, StorageResource storage, FileResource file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null, LimitClause? limit = null, OrderByClause? orderBy = null, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();
          file.ThrowIfDoesNotBelongTo(storage);

          if (userAuthenticationToken == null)
          {
            return list();
          }

          return userAuthenticationToken.Enter(list);

          IEnumerable<FileSnapshotResource> list()
          {
            _ = Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

            return Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), limit, orderBy, cancellationToken);
          }
        }
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long? BaseSnapshotId,
    long? AuthorFileAccessId,
    long? AuthorId
  ) : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long? BaseSnapshotId => Data.BaseSnapshotId;
  public long? AuthorFileAccessId => Data.AuthorFileAccessId;
  public long? AuthorId => Data.AuthorId;
}

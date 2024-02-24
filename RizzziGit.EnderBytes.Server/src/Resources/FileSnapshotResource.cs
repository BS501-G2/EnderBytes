using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileSnapshotResource(FileSnapshotResource.ResourceManager manager, FileSnapshotResource.ResourceData data) : Resource<FileSnapshotResource.ResourceManager, FileSnapshotResource.ResourceData, FileSnapshotResource>(manager, data)
{
  private const string NAME = "FileSnapshot";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceManager
  {
    private const string COLUMN_FILE_ID = "FileId";
    private const string COLUMN_BASE_SNAPSHOT_ID = "BaseSnapshotId";
    private const string COLUMN_AUTHOR_FILE_ACCESS_ID = "TokenId";
    private const string COLUMN_AUTHOR_ID = "AuthorId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Files.ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), cancellationToken);
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
      if (baseFileSnapshot == null)
      {
        return create();
      }

      lock (baseFileSnapshot)
      {
        baseFileSnapshot.ThrowIfInvalid();

        FileSnapshotResource fileSnapshot = create();

        foreach (FileBufferMapResource fileBufferMap in Service.FileBufferMaps.List(transaction, fileSnapshot, cancellationToken: cancellationToken))
        {
          FileBufferResource fileBuffer = Service.FileBuffers.GetById(transaction, fileBufferMap.Id, cancellationToken);

          Service.FileBufferMaps.Create(transaction, fileSnapshot, fileBuffer, fileBufferMap.Index, fileBufferMap.Length, cancellationToken);
        }

        return fileSnapshot;
      }

      FileSnapshotResource create()
      {
        (_, FileAccessResource? fileAccess) = Service.Storages.DecryptFileKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

        return Insert(transaction, new(
          (COLUMN_FILE_ID, file.Id),
          (COLUMN_BASE_SNAPSHOT_ID, baseFileSnapshot?.Id),
          (COLUMN_AUTHOR_FILE_ACCESS_ID, fileAccess?.Id),
          (COLUMN_AUTHOR_ID, userAuthenticationToken?.UserId)
        ), cancellationToken);
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

using System.Data.Common;
using RizzziGit.EnderBytes.Services;

namespace RizzziGit.EnderBytes.Resources;

public sealed class FileSnapshotResource(FileSnapshotResource.ResourceManager manager, FileSnapshotResource.ResourceData data) : Resource<FileSnapshotResource.ResourceManager, FileSnapshotResource.ResourceData, FileSnapshotResource>(manager, data)
{
  private const string NAME = "FileSnapshot";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceManager
  {
    private const string COLUMN_FILE_ID = "FileId";
    private const string COLUMN_BASE_SNAPSHOT_ID = "BaseSnapshotId";
    private const string COLUMN_AUTHOR_ID = "AuthorId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Files.ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), cancellationToken);
    }

    protected override FileSnapshotResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BASE_SNAPSHOT_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_ID} bigint not null;");
      }
    }

    public FileSnapshotResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource? baseSnapshot, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        if (baseSnapshot == null)
        {
          return create();
        }

        lock (baseSnapshot)
        {
          baseSnapshot.ThrowIfInvalid();

          return create();
        }

        FileSnapshotResource create()
        {
          return Insert(transaction, new(

          ), cancellationToken);
        }
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceData(Id, CreateTime, UpdateTime);
}

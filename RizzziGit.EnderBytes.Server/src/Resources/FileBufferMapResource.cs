using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class FileBufferMapResource(FileBufferMapResource.ResourceManager manager, FileBufferMapResource.ResourceData data) : Resource<FileBufferMapResource.ResourceManager, FileBufferMapResource.ResourceData, FileBufferMapResource>(manager, data)
{
  private const string NAME = "FileData";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileBufferMapResource>.ResourceManager
  {
    private const string COLUMN_FILE_ID = "FileId";
    private const string COLUMN_FILE_SNAPSHOT_ID = "FileSnapshotId";
    private const string COLUMN_FILE_BUFFER_ID = "FileBufferId";
    private const string COLUMN_INDEX = "BufferIndex";
    private const string COLUMN_LENGTH = "BufferLength";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.FileSnapshots.ResourceDeleted += (transaction, fileSnapshot, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id), cancellationToken);
    }

    protected override FileBufferMapResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_SNAPSHOT_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_BUFFER_ID)),
      reader.GetInt32(reader.GetOrdinal(COLUMN_INDEX)),
      reader.GetInt32(reader.GetOrdinal(COLUMN_LENGTH))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_SNAPSHOT_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_BUFFER_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INDEX} int not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_LENGTH} int not null;");
      }
    }

    public long GetReferenceCount(ResourceService.Transaction transaction, FileBufferResource fileBuffer, CancellationToken cancellationToken = default)
    {
      lock (fileBuffer)
      {
        fileBuffer.ThrowIfInvalid();

        return Count(transaction, new WhereClause.CompareColumn(COLUMN_FILE_BUFFER_ID, "=", fileBuffer.Id), cancellationToken);
      }
    }

    public IEnumerable<FileBufferMapResource> List(ResourceService.Transaction transaction, FileSnapshotResource snapshot, long startAtIndex = 0, CancellationToken cancellationToken = default)
    {
      lock (snapshot)
      {
        snapshot.ThrowIfInvalid();

        return Select(transaction, new WhereClause.Nested("and",
          new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", snapshot.Id),
          new WhereClause.CompareColumn(COLUMN_INDEX, ">=", startAtIndex)
        ), null, null, cancellationToken);
      }
    }

    public bool Update(ResourceService.Transaction transaction, FileBufferMapResource fileBufferMap, FileBufferResource? fileBuffer, CancellationToken cancellationToken = default)
    {
      lock (fileBufferMap)
      {
        fileBufferMap.ThrowIfInvalid();

        if (fileBuffer == null)
        {
          return create();
        }

        lock (fileBuffer)
        {
          fileBuffer.ThrowIfInvalid();

          return create();
        }

        bool create() => base.Update(transaction, fileBufferMap, new(
          (COLUMN_FILE_BUFFER_ID, fileBuffer?.Id)
        ), cancellationToken);
      }
    }

    public FileBufferMapResource Create(ResourceService.Transaction transaction, FileSnapshotResource snapshot, FileBufferResource? fileBuffer, int index, int size, CancellationToken cancellationToken = default)
    {
      lock (snapshot)
      {
        snapshot.ThrowIfInvalid();

        if (fileBuffer == null)
        {
          return create();
        }

        lock (fileBuffer)
        {
          fileBuffer.ThrowIfInvalid();

          return create();
        }

        FileBufferMapResource create()
        {
          return Insert(transaction, new(
            (COLUMN_FILE_ID, snapshot.FileId),
            (COLUMN_FILE_SNAPSHOT_ID, snapshot.Id),
            (COLUMN_FILE_BUFFER_ID, fileBuffer?.Id),
            (COLUMN_INDEX, index),
            (COLUMN_LENGTH, size)
          ), cancellationToken);
        }
      }
    }

    public void Truncate(ResourceService.Transaction transaction, FileBufferMapResource fileBufferMap, long startIndex, CancellationToken cancellationToken = default)
    {
      lock (fileBufferMap)
      {
        fileBufferMap.ThrowIfInvalid();
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    long FileSnapshotId,
    long? FileBufferId,
    int Index,
    int Length
  ) : Resource<ResourceManager, ResourceData, FileBufferMapResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long FileSnapshotId => Data.FileSnapshotId;
  public long? FileBufferId => Data.FileBufferId;
  public int Index => Data.Index;
  public int Length => Data.Length;
}

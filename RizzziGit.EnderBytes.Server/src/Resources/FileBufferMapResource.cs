using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Memory;

using Services;

public sealed class FileBufferMapResource(FileBufferMapResource.ResourceManager manager, FileBufferMapResource.ResourceData data) : Resource<FileBufferMapResource.ResourceManager, FileBufferMapResource.ResourceData, FileBufferMapResource>(manager, data)
{
  private const string NAME = "FileData";
  private const int VERSION = 1;

  private const int BUFFER_SIZE = 4_096;

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
      reader.GetInt64(reader.GetOrdinal(COLUMN_INDEX)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_LENGTH))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_SNAPSHOT_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_BUFFER_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INDEX} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_LENGTH} bigint not null;");
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

    public long GetSize(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();

          lock (fileSnapshot)
          {
            fileSnapshot.ThrowIfInvalid();

            return Select(transaction, new WhereClause.Nested("and",
              new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
              new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id)
            ), null, null, cancellationToken).Sum((fileBufferMap) => fileBufferMap.Length);
          }
        }
      }
    }

    public IEnumerable<FileBufferMapResource> Initialize(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();

          lock (fileSnapshot)
          {
            fileSnapshot.ThrowIfInvalid();

            if (fileSnapshot.BaseSnapshotId == null)
            {
              return [];
            }

            if (Count(transaction, new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id), cancellationToken) != 0)
            {
              throw new ArgumentException("Snapshot already has data.", nameof(fileSnapshot));
            }

            return Select(transaction, new WhereClause.Nested("and",
              new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
              new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.BaseSnapshotId)
            ), null, null, cancellationToken).ToList().Select((fileBufferMap) => Insert(transaction, new(
              (COLUMN_FILE_ID, fileBufferMap.FileId),
              (COLUMN_FILE_SNAPSHOT_ID, fileSnapshot.Id),
              (COLUMN_FILE_BUFFER_ID, fileBufferMap.FileBufferId),
              (COLUMN_INDEX, fileBufferMap.Index),
              (COLUMN_LENGTH, fileBufferMap.Length)
            ), cancellationToken)).ToList();
          }
        }
      }
    }

    public void Write(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, long offset, CompositeBuffer buffer, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();

          lock (fileSnapshot)
          {
            fileSnapshot.ThrowIfInvalid();

            lock (userAuthenticationToken)
            {
              userAuthenticationToken.ThrowIfInvalid();

              
            }
          }
        }
      }
    }

    public CompositeBuffer Read(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, long offset, long length, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();

          lock (fileSnapshot)
          {
            fileSnapshot.ThrowIfInvalid();

            lock (userAuthenticationToken)
            {
              userAuthenticationToken.ThrowIfInvalid();

              CompositeBuffer bytes = [];

              StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

              long beginIndex = offset / BUFFER_SIZE;
              {
                long bytesOffset = offset - (beginIndex * BUFFER_SIZE);
                foreach (FileBufferMapResource fileBufferMap in Select(transaction, new WhereClause.Nested("and",
                  new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
                  new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id),
                  new WhereClause.CompareColumn(COLUMN_INDEX, ">=", beginIndex)
                ), null, null, cancellationToken))
                {
                  if (fileBufferMap.FileBufferId == null)
                  {
                    bytes.Append(CompositeBuffer.Allocate(fileBufferMap.Length));
                  }
                  else
                  {
                    FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
                    bytes.Append(decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer));
                  }

                  if ((bytes.Length - bytesOffset) >= length)
                  {
                    break;
                  }
                }
                bytes.TruncateStart(bytesOffset);
              }

              if (bytes.Length > length)
              {
                bytes.Truncate(bytes.Length - length);
              }

              return bytes;
            }
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
    long FileSnapshotId,
    long? FileBufferId,
    long Index,
    long Length
  ) : Resource<ResourceManager, ResourceData, FileBufferMapResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long FileSnapshotId => Data.FileSnapshotId;
  public long? FileBufferId => Data.FileBufferId;
  public long Index => Data.Index;
  public long Length => Data.Length;
}

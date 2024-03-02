using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Memory;

using Services;

public sealed class FileBufferMapResource(FileBufferMapResource.ResourceManager manager, FileBufferMapResource.ResourceData data) : Resource<FileBufferMapResource.ResourceManager, FileBufferMapResource.ResourceData, FileBufferMapResource>(manager, data)
{
  public const string NAME = "FileBufferMap";
  public const int VERSION = 1;

  public const int BUFFER_SIZE = 1024 * 1024;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileBufferMapResource>.ResourceManager
  {
    public const string COLUMN_FILE_ID = "FileId";
    public const string COLUMN_FILE_SNAPSHOT_ID = "FileSnapshotId";
    public const string COLUMN_FILE_BUFFER_ID = "FileBufferId";
    public const string COLUMN_INDEX = "BufferIndex";
    public const string COLUMN_LENGTH = "BufferLength";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.FileSnapshots.ResourceDeleted += (transaction, fileSnapshot, cancellationToken) =>
      {
        foreach (FileBufferMapResource fileBufferMap in Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id), null, null, cancellationToken).ToList())
        {
          Delete(transaction, fileBufferMap, cancellationToken);
        }
      };
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

    public long GetReferenceCount(ResourceService.Transaction transaction, long fileBufferId, CancellationToken cancellationToken = default)
    {
      return Count(transaction, new WhereClause.CompareColumn(COLUMN_FILE_BUFFER_ID, "=", fileBufferId), cancellationToken);
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

            List<object?> parameters = [];
            WhereClause whereClause = new WhereClause.Nested("and",
              new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
              new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id)
            );

            return (long)(decimal)(SqlScalar(transaction, $"select coalesce(sum({COLUMN_LENGTH}), 0) as result from {NAME} where {whereClause.Apply(parameters)}", [.. parameters]) ?? 0);
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
            ), null, null, cancellationToken).ToList().Select((fileBufferMap) => InsertAndGet(transaction, new(
              (COLUMN_FILE_ID, file.Id),
              (COLUMN_FILE_SNAPSHOT_ID, fileSnapshot.Id),
              (COLUMN_FILE_BUFFER_ID, fileBufferMap.FileBufferId),
              (COLUMN_INDEX, fileBufferMap.Index),
              (COLUMN_LENGTH, fileBufferMap.Length)
            ), cancellationToken)).ToList();
          }
        }
      }
    }

    public void Write(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, long offset, CompositeBuffer buffer, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (file)
        {
          lock (fileSnapshot)
          {
            StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

            fileSnapshot.ThrowIfInvalid();

            if (userAuthenticationToken == null)
            {
              write();
              return;
            }

            userAuthenticationToken.Enter(write);

            void write()
            {
              CompositeBuffer bytes = [];
              long beginIndex = offset / BUFFER_SIZE;
              long bytesOffset = offset - (beginIndex * BUFFER_SIZE);

              ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, GetSize(transaction, storage, file, fileSnapshot, cancellationToken), nameof(offset));

              foreach (FileBufferMapResource fileBufferMap in Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToList())
              {
                cancellationToken.ThrowIfCancellationRequested();

                if (fileBufferMap.FileBufferId == null)
                {
                  bytes.Append(CompositeBuffer.Allocate(fileBufferMap.Length));
                }
                else if (fileBufferMap.CachedBuffer != null)
                {
                  bytes.Append(fileBufferMap.CachedBuffer);
                }
                else
                {
                  FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
                  bytes.Append(fileBufferMap.CachedBuffer = decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer));
                }

                if ((bytes.Length - bytesOffset) >= buffer.Length)
                {
                  break;
                }
              }

              CompositeBuffer removedTemp = bytes.SpliceStart(bytesOffset);

              if (bytes.Length < buffer.Length)
              {
                bytes.Write(0, buffer.Slice(0, bytes.Length));
                bytes.Append(buffer.Slice(bytes.Length));
              }
              else
              {
                bytes.Write(0, buffer);
              }

              bytes.Prepend(removedTemp);

              for (long index = beginIndex; bytes.Length > 0; index++)
              {
                cancellationToken.ThrowIfCancellationRequested();

                CompositeBuffer fileBufferData = bytes.SpliceStart(long.Min(bytes.Length, BUFFER_SIZE));
                long fileBufferId = Service.FileBuffers.Create(transaction, file, decryptedKeyInfo.Key.Encrypt(fileBufferData.ToByteArray()), cancellationToken);

                FileBufferMapResource? fileBufferMap = Select(transaction, file, fileSnapshot, index, new(1), cancellationToken).FirstOrDefault();

                if (fileBufferMap == null)
                {
                  fileBufferMap = InsertAndGet(transaction, new(
                    (COLUMN_FILE_ID, file.Id),
                    (COLUMN_FILE_SNAPSHOT_ID, fileSnapshot.Id),
                    (COLUMN_FILE_BUFFER_ID, fileBufferId),
                    (COLUMN_INDEX, index),
                    (COLUMN_LENGTH, fileBufferData.Length)
                  ), cancellationToken);
                }
                else
                {
                  Update(transaction, fileBufferMap, fileBufferId, index, fileBufferData.Length, cancellationToken);
                }

                fileBufferMap.CachedBuffer = fileBufferData.ToByteArray();
              }
            }
          }
        }
      }
    }

    public CompositeBuffer Read(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, long offset, long length, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (file)
        {
          lock (fileSnapshot)
          {
            StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

            fileSnapshot.ThrowIfInvalid();

            if (userAuthenticationToken == null)
            {
              return read();
            }

            return userAuthenticationToken.Enter(read);

            CompositeBuffer read()
            {

              CompositeBuffer bytes = [];
              long beginIndex = offset / BUFFER_SIZE;
              long bytesOffset = offset - (beginIndex * BUFFER_SIZE);

              foreach (FileBufferMapResource fileBufferMap in Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToList())
              {
                cancellationToken.ThrowIfCancellationRequested();

                if (fileBufferMap.FileBufferId == null)
                {
                  bytes.Append(CompositeBuffer.Allocate(fileBufferMap.Length));
                }
                else if (fileBufferMap.CachedBuffer != null)
                {
                  bytes.Append(fileBufferMap.CachedBuffer);
                }
                else
                {
                  FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
                  bytes.Append(fileBufferMap.CachedBuffer = decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer));
                }

                if ((bytes.Length - bytesOffset) >= length)
                {
                  break;
                }
              }

              return bytes.Slice(bytesOffset, long.Min(bytesOffset + length, bytes.Length));
            }
          }
        }
      }
    }

    public void Truncate(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, long length, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (file)
        {
          lock (fileSnapshot)
          {
            StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

            fileSnapshot.ThrowIfInvalid();

            if (userAuthenticationToken == null)
            {
              truncate();
              return;
            }

            userAuthenticationToken.Enter(truncate);
            return;

            void truncate()
            {
              long beginIndex = length / BUFFER_SIZE;
              long bytesOffset = length - (BUFFER_SIZE * beginIndex);

              ArgumentOutOfRangeException.ThrowIfGreaterThan(length, GetSize(transaction, storage, file, fileSnapshot, cancellationToken), nameof(length));

              foreach (FileBufferMapResource fileBufferMap in Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToList())
              {
                if (fileBufferMap.Index != beginIndex)
                {
                  Delete(transaction, fileBufferMap, cancellationToken);
                  continue;
                }

                CompositeBuffer bytes;

                if (fileBufferMap.FileBufferId == null)
                {
                  bytes = CompositeBuffer.Allocate(bytesOffset);
                }
                else
                {
                  FileBufferResource oldFileBuffer = Service.FileBuffers.GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
                  bytes = CompositeBuffer.From(decryptedKeyInfo.Key.Decrypt(oldFileBuffer.Buffer)).Slice(0, bytesOffset);
                }

                Update(transaction, fileBufferMap, Service.FileBuffers.Create(transaction, file, decryptedKeyInfo.Key.Encrypt(bytes.ToByteArray()), cancellationToken), fileBufferMap.Index, bytes.Length, cancellationToken);
              }
            }
          }
        }
      }
    }

    private IEnumerable<FileBufferMapResource> Select(ResourceService.Transaction transaction, FileResource file, FileSnapshotResource fileSnapshot, long startIndex, LimitClause? limit = null, CancellationToken cancellationToken = default)
    {
      return Select(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id),
        new WhereClause.CompareColumn(COLUMN_INDEX, ">=", startIndex)
      ), limit, null, cancellationToken);
    }

    private bool Update(ResourceService.Transaction transaction, FileBufferMapResource fileBufferMap, long? fileBufferId, long index, long length, CancellationToken cancellationToken = default)
    {
      try
      {
        return Update(transaction, fileBufferMap, new(
          (COLUMN_FILE_BUFFER_ID, fileBufferId),
          (COLUMN_INDEX, index),
          (COLUMN_LENGTH, length)
        ), cancellationToken);
      }
      finally
      {
        Service.FileBuffers.DeleteIfNotReferenced(transaction, fileBufferMap.FileBufferId, cancellationToken);
      }
    }

    public override bool Delete(ResourceService.Transaction transaction, FileBufferMapResource fileBufferMap, CancellationToken cancellationToken = default)
    {
      try
      {
        return base.Delete(transaction, fileBufferMap, cancellationToken);
      }
      finally
      {
        Service.FileBuffers.DeleteIfNotReferenced(transaction, fileBufferMap.FileBufferId, cancellationToken);
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

  private byte[]? CachedBuffer;
}

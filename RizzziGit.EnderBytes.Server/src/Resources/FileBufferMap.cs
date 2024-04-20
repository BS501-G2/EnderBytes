using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using System.Runtime.CompilerServices;
using Commons.Memory;
using Services;

public sealed class FileBufferMapManager : ResourceManager<FileBufferMapManager, FileBufferMapManager.Resource, FileBufferMapManager.Exception>
{
  public abstract class Exception(string? message = null) : ResourceService.Exception(message);

  public class SnapshotAlreadyInitializedException(FileManager.Resource file, FileSnapshotManager.Resource snapshot) : Exception("Snapshot is already initialized.")
  {
    public readonly FileManager.Resource File = file;
    public readonly FileSnapshotManager.Resource Snapshot = snapshot;
  }

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    long FileSnapshotId,
    long? FileBufferId,
    long Index,
    long Length
  ) : ResourceManager<FileBufferMapManager, Resource, Exception>.Resource(Id, CreateTime, UpdateTime)
  {
    public byte[]? CachedBuffer = null;
  }

  public const string NAME = "FileBufferMap";
  public const int VERSION = 1;

  public const int BUFFER_SIZE = 1024 * 1024;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_FILE_SNAPSHOT_ID = "FileSnapshotId";
  public const string COLUMN_FILE_BUFFER_ID = "FileBufferId";
  public const string COLUMN_INDEX = "BufferIndex";
  public const string COLUMN_LENGTH = "BufferLength";

  public FileBufferMapManager(ResourceService service) : base(service, NAME, VERSION)
  {
    Service.GetManager<FileSnapshotManager>().RegisterDeleteHandler(async (transaction, fileSnapshot, cancellationToken) =>
    {
      await foreach (Resource fileBufferMap in Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id), null, null, cancellationToken))
      {
        await Delete(transaction, fileBufferMap, cancellationToken);
      }
    });
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_SNAPSHOT_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_BUFFER_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_INDEX)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_LENGTH))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_SNAPSHOT_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_BUFFER_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INDEX} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_LENGTH} bigint not null;");
    }
  }

  public async Task<long> GetReferenceCount(ResourceService.Transaction transaction, long fileBufferId, CancellationToken cancellationToken = default)
  {
    return await Count(transaction, new WhereClause.CompareColumn(COLUMN_FILE_BUFFER_ID, "=", fileBufferId), cancellationToken);
  }

  public async Task<long> GetSize(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, CancellationToken cancellationToken = default)
  {
    List<object?> parameters = [];
    WhereClause whereClause = new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id)
    );

    return (long)(decimal)(await SqlScalar(transaction, $"select coalesce(sum({COLUMN_LENGTH}), 0) as result from {NAME} where {whereClause.Apply(parameters)}", [.. parameters]) ?? 0);
  }

  public async Task Initialize(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, CancellationToken cancellationToken = default)
  {
    if (fileSnapshot.BaseSnapshotId == null)
    {
      return;
    }

    if (await Count(transaction, new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id), cancellationToken) != 0)
    {
      throw new SnapshotAlreadyInitializedException(file, fileSnapshot);
    }

    await foreach (Resource fileBufferMap in Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.BaseSnapshotId)
    ), null, null, cancellationToken))
    {
      await InsertAndGet(transaction, new(
        (COLUMN_FILE_ID, file.Id),
        (COLUMN_FILE_SNAPSHOT_ID, fileSnapshot.Id),
        (COLUMN_FILE_BUFFER_ID, fileBufferMap.FileBufferId),
        (COLUMN_INDEX, fileBufferMap.Index),
        (COLUMN_LENGTH, fileBufferMap.Length)
      ), cancellationToken);
    }
  }

  public async Task Write(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, long offset, CompositeBuffer buffer, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    DecryptedKeyInfo decryptedKeyInfo = await transaction.ResourceService.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken);

    CompositeBuffer bytes = [];
    long beginIndex = offset / BUFFER_SIZE;
    long bytesOffset = offset - (beginIndex * BUFFER_SIZE);

    ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, await GetSize(transaction, storage, file, fileSnapshot, cancellationToken), nameof(offset));

    foreach (Resource fileBufferMap in await Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToListAsync(cancellationToken))
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
        FileBufferManager.Resource? fileBuffer = await transaction.ResourceService.GetManager<FileBufferManager>().GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
        if (fileBuffer == null)
        {
          throw new NotFoundException(NAME, (long)fileBufferMap.FileBufferId);
        }

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
      long fileBufferId = await Service.GetManager<FileBufferManager>().Create(transaction, file, decryptedKeyInfo.Key.Encrypt(fileBufferData.ToByteArray()), cancellationToken);

      Resource? fileBufferMap = await Select(transaction, file, fileSnapshot, index, new(1), cancellationToken).FirstOrDefaultAsync(cancellationToken);

      if (fileBufferMap == null)
      {
        fileBufferMap = await InsertAndGet(transaction, new(
          (COLUMN_FILE_ID, file.Id),
          (COLUMN_FILE_SNAPSHOT_ID, fileSnapshot.Id),
          (COLUMN_FILE_BUFFER_ID, fileBufferId),
          (COLUMN_INDEX, index),
          (COLUMN_LENGTH, fileBufferData.Length)
        ), cancellationToken);
      }
      else
      {
        await Update(transaction, fileBufferMap, fileBufferId, index, fileBufferData.Length, cancellationToken);
      }

      fileBufferMap.CachedBuffer = fileBufferData.ToByteArray();
    }
  }

  public async Task<CompositeBuffer> Read(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, long offset, long length, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    DecryptedKeyInfo decryptedKeyInfo = await transaction.ResourceService.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

    CompositeBuffer bytes = [];
    long beginIndex = offset / BUFFER_SIZE;
    long bytesOffset = offset - (beginIndex * BUFFER_SIZE);

    foreach (Resource fileBufferMap in await Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToListAsync(cancellationToken))
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
        FileBufferManager.Resource? fileBuffer = await transaction.ResourceService.GetManager<FileBufferManager>().GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
        if (fileBuffer == null)
        {
          throw new NotFoundException(NAME, (long)fileBufferMap.FileBufferId);
        }
        bytes.Append(fileBufferMap.CachedBuffer = decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer));
      }

      if ((bytes.Length - bytesOffset) >= length)
      {
        break;
      }
    }

    return bytes.Slice(bytesOffset, long.Min(bytesOffset + length, bytes.Length));
  }

  public async Task Truncate(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, long length, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    DecryptedKeyInfo decryptedKeyInfo = await transaction.ResourceService.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);
    long beginIndex = length / BUFFER_SIZE;
    long bytesOffset = length - (BUFFER_SIZE * beginIndex);

    ArgumentOutOfRangeException.ThrowIfGreaterThan(length, await GetSize(transaction, storage, file, fileSnapshot, cancellationToken), nameof(length));

    foreach (Resource fileBufferMap in await Select(transaction, file, fileSnapshot, beginIndex, null, cancellationToken).ToListAsync(cancellationToken))
    {
      if (fileBufferMap.Index != beginIndex)
      {
        await Delete(transaction, fileBufferMap, cancellationToken);
        continue;
      }

      CompositeBuffer bytes;

      if (fileBufferMap.FileBufferId == null)
      {
        bytes = CompositeBuffer.Allocate(bytesOffset);
      }
      else
      {
        FileBufferManager.Resource? oldFileBuffer = await transaction.ResourceService.GetManager<FileBufferManager>().GetById(transaction, (long)fileBufferMap.FileBufferId, cancellationToken);
        if (oldFileBuffer == null)
        {
          throw new NotFoundException(NAME, (long)fileBufferMap.FileBufferId);
        }
        bytes = CompositeBuffer.From(decryptedKeyInfo.Key.Decrypt(oldFileBuffer.Buffer)).Slice(0, bytesOffset);
      }

      await Update(transaction, fileBufferMap, await Service.GetManager<FileBufferManager>().Create(transaction, file, decryptedKeyInfo.Key.Encrypt(bytes.ToByteArray()), cancellationToken), fileBufferMap.Index, bytes.Length, cancellationToken);
    }
  }

  private IAsyncEnumerable<Resource> Select(ResourceService.Transaction transaction, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, long startIndex, LimitClause? limit = null, CancellationToken cancellationToken = default)
  {
    return Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_FILE_SNAPSHOT_ID, "=", fileSnapshot.Id),
      new WhereClause.CompareColumn(COLUMN_INDEX, ">=", startIndex)
    ), limit, null, cancellationToken);
  }

  private async Task<bool> Update(ResourceService.Transaction transaction, Resource fileBufferMap, long? fileBufferId, long index, long length, CancellationToken cancellationToken = default)
  {
    try
    {
      return await Update(transaction, fileBufferMap, new(
        (COLUMN_FILE_BUFFER_ID, fileBufferId),
        (COLUMN_INDEX, index),
        (COLUMN_LENGTH, length)
      ), cancellationToken);
    }
    finally
    {
      await Service.GetManager<FileBufferManager>().DeleteIfNotReferenced(transaction, fileBufferMap.FileBufferId, cancellationToken);
    }
  }

  public override async Task<bool> Delete(ResourceService.Transaction transaction, Resource fileBufferMap, CancellationToken cancellationToken = default)
  {
    try
    {
      return await base.Delete(transaction, fileBufferMap, cancellationToken);
    }
    finally
    {
      await Service.GetManager<FileBufferManager>().DeleteIfNotReferenced(transaction, fileBufferMap.FileBufferId, cancellationToken);
    }
  }
}

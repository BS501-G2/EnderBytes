using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;
using System.Runtime.CompilerServices;

public sealed class FileSnapshotManager : ResourceManager<FileSnapshotManager, FileSnapshotManager.Resource, FileSnapshotManager.Exception>
{
  public abstract class Exception(string? message = null) : ResourceService.Exception(message);

  public new sealed record Resource(
      long Id,
      long CreateTime,
      long UpdateTime,
      long FileId,
      long? BaseSnapshotId,
      long? AuthorFileAccessId,
      long? AuthorId
  ) : ResourceManager<FileSnapshotManager, Resource, Exception>.Resource(Id, CreateTime, UpdateTime)
  {
  }

  public const string NAME = "FileSnapshot";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_BASE_SNAPSHOT_ID = "BaseSnapshotId";
  public const string COLUMN_AUTHOR_FILE_ACCESS_ID = "TokenId";
  public const string COLUMN_AUTHOR_ID = "AuthorId";

  public FileSnapshotManager(ResourceService service) : base(service, NAME, VERSION)
  {
    Service.GetManager<FileManager>().RegisterDeleteHandler((transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), cancellationToken));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_BASE_SNAPSHOT_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_AUTHOR_FILE_ACCESS_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_AUTHOR_ID))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BASE_SNAPSHOT_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_FILE_ACCESS_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_ID} bigint null;");
    }
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, Resource? baseFileSnapshot, UserAuthenticationToken? userAuthenticationToken = null, CancellationToken cancellationToken = default)
  {
    (_, FileAccessManager.Resource? fileAccess) = await Service.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken);

    Resource fileSnapshot = await InsertAndGet(transaction, new(
      (COLUMN_FILE_ID, file.Id),
      (COLUMN_BASE_SNAPSHOT_ID, baseFileSnapshot?.Id),
      (COLUMN_AUTHOR_FILE_ACCESS_ID, fileAccess?.Id),
      (COLUMN_AUTHOR_ID, userAuthenticationToken?.UserId)
    ), cancellationToken);

    if (baseFileSnapshot != null)
    {
      await Service.GetManager<FileBufferMapManager>().Initialize(transaction, storage, file, fileSnapshot, cancellationToken);
    }

    return fileSnapshot;
  }

  public async IAsyncEnumerable<Resource> List(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, UserAuthenticationToken? userAuthenticationToken = null, LimitClause? limit = null, OrderByClause? orderBy = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    file.ThrowIfDoesNotBelongTo(storage);
    _ = await Service.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

    await foreach(var snapshot in Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id), limit, orderBy, cancellationToken))
    {
      yield return snapshot;
    }
  }
}

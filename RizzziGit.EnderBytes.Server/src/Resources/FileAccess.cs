using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

public enum FileAccessTargetEntityType : byte { None, User }
public enum FileAccessType : byte { ManageShares, ReadWrite, Read, None }

public sealed class FileAccessManager : ResourceManager<FileAccessManager, FileAccessManager.Resource, FileAccessManager.Exception>
{
  public new abstract class Exception(string? message = null) : ResourceService.ResourceManager.Exception(message);

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,
    long TargetFileId,
    long TargetEntityId,
    FileAccessTargetEntityType TargetEntityType,
    byte[] AesKey,
    FileAccessType Type
  ) : ResourceManager<FileAccessManager, Resource, Exception>.Resource(Id, CreateTime, UpdateTime)
  {
    [JsonIgnore]
    public byte[] AesKey = AesKey;
  }

  private const string NAME = "FileAccess";
  private const int VERSION = 1;

  public const string COLUMN_TARGET_FILE_ID = "TargetFileId";
  public const string COLUMN_TARGET_ENTITY_ID = "TargetEntityId";
  public const string COLUMN_TARGET_ENTITY_TYPE = "TargetEntityType";
  public const string COLUMN_KEY = "AesKey";
  public const string COLUMN_TYPE = "Type";

  public FileAccessManager(ResourceService service) : base(service, NAME, VERSION)
  {
    Service.GetManager<FileManager>().RegisterDeleteHandler((transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id), cancellationToken));
    Service.GetManager<UserManager>().RegisterDeleteHandler((transaction, user, cancellationToken) => Delete(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", (byte)FileAccessTargetEntityType.User),
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id)
    ), cancellationToken));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_FILE_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_ENTITY_ID)),
    (FileAccessTargetEntityType)reader.GetByte(reader.GetOrdinal(COLUMN_TARGET_ENTITY_TYPE)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),
    (FileAccessType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_TYPE} tinyint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");
    }
  }

  public async IAsyncEnumerable<Resource> List(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource file, LimitClause? limit = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    file.ThrowIfDoesNotBelongTo(storage);

    await foreach (var fileAccess in Select(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id), limit, new OrderByClause(COLUMN_TYPE, OrderByClause.OrderBy.Ascending), cancellationToken))
    {
      yield return fileAccess;
    }
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource targetFile, UserManager.Resource targetUser, FileAccessType type, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    targetFile.ThrowIfDoesNotBelongTo(storage);

    KeyService.AesPair fileKey = (await transaction.ResourceService.GetManager<StorageManager>().DecryptKey(transaction, storage, targetFile, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken)).Key;

    return await InsertAndGet(transaction, new(
      (COLUMN_TARGET_FILE_ID, targetFile.Id),
      (COLUMN_TARGET_ENTITY_ID, targetUser.Id),
      (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.User),
      (COLUMN_KEY, targetUser.Encrypt(Service.Server.KeyService, fileKey.Serialize())),
      (COLUMN_TYPE, (byte)type)
    ), cancellationToken);
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, StorageManager.Resource storage, FileManager.Resource targetFile, FileAccessType type, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    targetFile.ThrowIfDoesNotBelongTo(storage);

    KeyService.AesPair fileKey = (await transaction.ResourceService.GetManager<StorageManager>().DecryptKey(transaction, storage, targetFile, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken)).Key;

    return await InsertAndGet(transaction, new(
      (COLUMN_TARGET_FILE_ID, targetFile.Id),
      (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.None),
      (COLUMN_KEY, fileKey.Serialize()),
      (COLUMN_TYPE, (byte)type)
    ), cancellationToken);
  }
}

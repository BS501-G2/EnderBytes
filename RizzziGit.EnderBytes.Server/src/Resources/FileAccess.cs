using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileAccessManager, FileAccessManager.Resource>;

public enum FileAccessTargetEntityType
{
  User, None
}

public enum FileAccessExtent
{
  None, ReadOnly, ReadWrite, ManageAccess, Full
}

public sealed record FileAccessPoint(FileAccessManager.Resource AccessPoint, FileManager.Resource[] PathChain);

public sealed class FileAccessManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    FileAccessTargetEntityType TargetEntityType,
    long? TargetEntityId,
    long TargetFileId,

    byte[] TargetFileAesKey,
    FileAccessExtent FileAccessExtent
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "FileAccess";
  public const int VERSION = 1;

  private const string COLUMN_TARGET_ENTITY_TYPE = "TargetEntityType";
  private const string COLUMN_TARGET_ENTITY_ID = "TargetEntityId";
  private const string COLUMN_TARGET_FILE_ID = "TargetFileId";
  private const string COLUMN_TARGET_FILE_AES_KEY = "TargetFileAesKey";
  private const string COLUMN_EXTENT = "Extent";

  public FileAccessManager(ResourceService service) : base(service, NAME, VERSION)
  {
    GetManager<UserManager>().RegisterDeleteHandler((transaction, user) => Delete(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", FileAccessTargetEntityType.User)
    )));

    GetManager<FileManager>().RegisterDeleteHandler((transaction, file) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id)));
  }

  public KeyService KeyService => Service.Server.KeyService;

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    (FileAccessTargetEntityType)reader.GetByte(reader.GetOrdinal(COLUMN_TARGET_ENTITY_TYPE)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TARGET_ENTITY_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_FILE_ID)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_TARGET_FILE_AES_KEY)),
    (FileAccessExtent)reader.GetByte(reader.GetOrdinal(COLUMN_EXTENT))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_TYPE} tinyint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_FILE_AES_KEY} blob not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_EXTENT} tinyint not null;");
    }
  }

  public async Task<Resource> GrantUser(ResourceService.Transaction transaction, FileManager.Resource file, UserManager.Resource user, KeyService.AesPair fileKey, FileAccessExtent extent = FileAccessExtent.ReadOnly)
  {
    return await InsertAndGet(transaction, new(
      (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.User),
      (COLUMN_TARGET_ENTITY_ID, user.Id),
      (COLUMN_TARGET_FILE_ID, file.Id),
      (COLUMN_TARGET_FILE_AES_KEY, user.Encrypt(KeyService, fileKey.Serialize())),
      (COLUMN_EXTENT, (byte)extent)
    ));
  }

  public async Task<bool> RevokeUser(ResourceService.Transaction transaction, FileManager.Resource file, UserManager.Resource user, FileAccessExtent? extent = null)
  {
    return await Delete(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", FileAccessTargetEntityType.User),
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
      new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id),
      extent == null ? null : new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
    )) != 0;
  }

  public async Task<Resource[]> ListByFile(ResourceService.Transaction transaction, FileManager.Resource file, FileAccessExtent? extent = null)
  {
    return await Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id),
      extent == null ? null : new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
    ));
  }

  public async Task<Resource[]> ListByUser(ResourceService.Transaction transaction, UserManager.Resource user, FileAccessExtent? extent = null)
  {
    return await Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", FileAccessTargetEntityType.User),
      new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
      extent == null ? null : new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
    ));
  }

  public async Task<FileAccessPoint?> GetAccessPoint(ResourceService.Transaction transaction, UserManager.Resource user, FileManager.Resource file, FileAccessExtent? extent)
  {
    FileManager.Resource[] pathChain = await GetManager<FileManager>().PathChain(transaction, file);

    foreach (FileManager.Resource entry in pathChain)
    {
      Resource? accessPoint = await SelectFirst(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", FileAccessTargetEntityType.User),
        new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
        new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", entry.Id),
        extent == null ? null : new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
      ), [new OrderByClause(COLUMN_EXTENT, OrderByClause.OrderBy.Descending)]);

      if (accessPoint != null)
      {
        return new(accessPoint, pathChain);
      }
    }

    return null;
  }
}

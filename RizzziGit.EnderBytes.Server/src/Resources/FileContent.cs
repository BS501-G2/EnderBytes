using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using RizzziGit.Commons.Memory;
using Services;

using ResourceManager = ResourceManager<FileContentManager, FileContentManager.Resource>;

public sealed class FileContentManager : ResourceManager
{
  public const string NAME = "FileContent";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_IS_ROOT = "IsRoot";

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long FileId,
    bool IsRoot
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public new sealed class Exception(string? message = null) : ResourceService.ResourceManager.Exception(message);

  public FileContentManager(ResourceService service) : base(service, NAME, VERSION)
  {
    GetManager<FileManager>().RegisterDeleteHandler((transaction, file) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id)));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetBoolean(reader.GetOrdinal(COLUMN_IS_ROOT))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_IS_ROOT} tinyint(1) not null;");
    }
  }

  public async Task<Resource> GetRootFileMetadata(ResourceService.Transaction transaction, long fileId)
  {
    return (
      await SelectFirst(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", fileId),
        new WhereClause.CompareColumn(COLUMN_IS_ROOT, "=", true)
      )) ??
      await InsertAndGet(transaction, new(
        (COLUMN_FILE_ID, fileId),
        (COLUMN_IS_ROOT, true)
      ))
    );
  }

  public Task<Resource> Create(ResourceService.Transaction transaction, long fileId) => InsertAndGet(transaction, new(
    (COLUMN_FILE_ID, fileId),
    (COLUMN_IS_ROOT, false)
  ));
}

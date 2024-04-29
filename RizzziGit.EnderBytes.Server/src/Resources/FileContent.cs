using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileContentManager, FileContentManager.Resource>;

public sealed class FileContentManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    bool IsMain
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "FileContent";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_IS_MAIN = "IsMain";

  public FileContentManager(ResourceService service) : base(service, NAME, VERSION)
  {
    GetManager<FileManager>().RegisterDeleteHandler(async (transaction, file) =>
    {
      await Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id));
    });
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
    reader.GetBoolean(reader.GetOrdinal(COLUMN_IS_MAIN))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_IS_MAIN} tinyint(1) not null;");
    }
  }

  public async Task<Resource> GetMainContent(ResourceService.Transaction transaction, FileManager.Resource file)
  {
    Resource? fileContent;

    if ((fileContent = await SelectFirst(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_IS_MAIN, "=", true)
    ))) == null)
    {
      fileContent = await InsertAndGet(transaction, new(
        (COLUMN_FILE_ID, file.Id),
        (COLUMN_IS_MAIN, true)
      ));
    }

    return fileContent;
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, FileManager.Resource file)
  {
    return await InsertAndGet(transaction, new(
      (COLUMN_FILE_ID, file.Id),
      (COLUMN_IS_MAIN, false)
    ));
  }

  public async Task<Resource[]> List(ResourceService.Transaction transaction, FileManager.Resource file)
  {
    return await Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id));
  }
}

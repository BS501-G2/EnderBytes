using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileContentVersionManager, FileContentVersionManager.Resource>;

public sealed class FileContentVersionManager : ResourceManager
{
	public const string NAME = "FileContentVersion";
	public const int VERSION = 1;

	public const string COLUMN_FILE_CONTENT_ID = "FileContentId";
	public const string COLUMN_BASE_VERSION_ID = "BaseVersionId";

	public new sealed record Resource(
		long Id,
		long CreateTime,
		long UpdateTime,

		long FileContentId,
		long? BaseVersionId
	) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

	public FileContentVersionManager(ResourceService service) : base(service, NAME, VERSION)
	{
		GetManager<FileContentManager>().RegisterDeleteHandler(async (transaction, resource) =>
		{
			await Delete(transaction, new WhereClause.Nested("and",
			new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", resource.Id)
			));
		});
	}

	protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
	id, createTime, updateTime,

	reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_CONTENT_ID)),
	reader.GetInt64Optional(reader.GetOrdinal(COLUMN_BASE_VERSION_ID))
	);

	protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
	{
		if (oldVersion < 1)
		{
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_CONTENT_ID} bigint not null;");
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BASE_VERSION_ID} bigint null;");
		}
	}

	public IAsyncEnumerable<Resource> List(ResourceService.Transaction transaction, FileContentManager.Resource fileContent)
	{
		return Select(transaction, new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id));
	}

	public async Task<Resource> GetBaseVersion(ResourceService.Transaction transaction, FileContentManager.Resource fileContent)
	{
		return await SelectFirst(transaction, new WhereClause.Nested("and",
			new WhereClause.CompareColumn(COLUMN_FILE_CONTENT_ID, "=", fileContent.Id),
			new WhereClause.Raw($"{COLUMN_BASE_VERSION_ID} is null")
		)) ?? await InsertAndGet(transaction, new(
			(COLUMN_FILE_CONTENT_ID, fileContent.Id),
			(COLUMN_BASE_VERSION_ID, null)
		));
	}
}

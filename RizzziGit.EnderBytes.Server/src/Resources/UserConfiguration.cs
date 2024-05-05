using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserConfigurationManager : ResourceManager<UserConfigurationManager, UserConfigurationManager.Resource>
{
	public new sealed record Resource(
		long Id,
		long CreateTime,
		long UpdateTime,

		long UserId,
		bool EnableFtpAccess
	) : ResourceManager<UserConfigurationManager, Resource>.Resource(Id, CreateTime, UpdateTime)
	{
	}

	public const string NAME = "UserConfiguration";
	public const int VERSION = 1;

	public const string COLUMN_USER_ID = "UserId";
	public const string COLUMN_ENABLE_FTP_ACCESS = "EnableFTPAccess";

	public const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

	public UserConfigurationManager(ResourceService service) : base(service, NAME, VERSION)
	{
		service.GetManager<UserManager>().RegisterDeleteHandler((transaction, user) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id)));
	}

	protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
	id, createTime, updateTime,

	reader.GetInt64(reader.GetOrdinal(COLUMN_USER_ID)),
	reader.GetBoolean(reader.GetOrdinal(COLUMN_ENABLE_FTP_ACCESS))
	);

	protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
	{
		if (oldVersion < 1)
		{
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_ID} bigint not null;");
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENABLE_FTP_ACCESS} bigint not null;");

			await SqlNonQuery(transaction, $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});");
		}
	}

	public async Task<Resource> Get(ResourceService.Transaction transaction, UserManager.Resource user)
	{
		await foreach (Resource configuration in Select(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id)))
		{
			return configuration;
		}

		return await InsertAndGet(transaction, new(
			(COLUMN_USER_ID, user.Id),
			(COLUMN_ENABLE_FTP_ACCESS, false)
		));
	}
}

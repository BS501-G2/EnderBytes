using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserAuthenticationSessionTokenManager : ResourceManager<UserAuthenticationSessionTokenManager, UserAuthenticationSessionTokenManager.Resource>
{
	public new sealed record Resource(
		long Id,
		long CreateTime,
		long UpdateTime,
		long AuthenticationId,
		long ExpiryTime
	) : ResourceManager<UserAuthenticationSessionTokenManager, Resource>.Resource(Id, CreateTime, UpdateTime)
	{
		public bool Expired => ExpiryTime <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	public const string NAME = "UserAuthenticationSessionToken";
	public const int VERSION = 1;

	public const string COLUMN_USER_AUTHENTICATION_ID = "UserAuthenticationId";
	public const string COLUMN_EXPIRY_TIME = "ExpiryTime";

	public UserAuthenticationSessionTokenManager(ResourceService service) : base(service, NAME, VERSION)
	{
		service.GetManager<UserAuthenticationManager>().RegisterDeleteHandler((transaction, resource) =>
		{
			return Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_AUTHENTICATION_ID, "=", resource.Id));
		});
	}

	protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
	id, createTime, updateTime,

	reader.GetInt64(reader.GetOrdinal(COLUMN_USER_AUTHENTICATION_ID)),
	reader.GetInt64(reader.GetOrdinal(COLUMN_EXPIRY_TIME))
	);

	protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
	{
		if (oldVersion < 1)
		{
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_AUTHENTICATION_ID} bigint not null;");
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_EXPIRY_TIME} bigint not null;");
		}
	}

	public async Task<Resource> Create(ResourceService.Transaction transaction, UserAuthenticationManager.Resource userAuthentication, long expireTimer)
	{
		if (userAuthentication.Type != UserAuthenticationType.SessionToken)
		{
			throw new ArgumentException("Invalid user authentication type.", nameof(userAuthentication));
		}

		return await InsertAndGet(transaction, new(
			(COLUMN_USER_AUTHENTICATION_ID, userAuthentication.Id),
			(COLUMN_EXPIRY_TIME, userAuthentication.CreateTime + expireTimer)
		));
	}

	public async Task<Resource> GetByUserAuthentication(ResourceService.Transaction transaction, UserAuthenticationManager.Resource userAuthentication)
	{
		if (userAuthentication.Type != UserAuthenticationType.SessionToken)
		{
			throw new ArgumentException("Invalid user authentication type.", nameof(userAuthentication));
		}

		return (await SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_USER_AUTHENTICATION_ID, "=", userAuthentication.Id)))!;
	}

	public async Task<bool> ResetExpiryTime(ResourceService.Transaction transaction, Resource userAuthenticationSessionToken, long expireTimer)
	{
		return await Update(transaction, userAuthenticationSessionToken, new(
			(COLUMN_EXPIRY_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + expireTimer)
		));
	}
}

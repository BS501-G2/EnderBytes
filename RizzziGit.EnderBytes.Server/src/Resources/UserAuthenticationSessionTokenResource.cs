using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserAuthenticationSessionTokenResource(UserAuthenticationSessionTokenResource.ResourceManager manager, UserAuthenticationSessionTokenResource.ResourceData data) : Resource<UserAuthenticationSessionTokenResource.ResourceManager, UserAuthenticationSessionTokenResource.ResourceData, UserAuthenticationSessionTokenResource>(manager, data)
{
  public const string NAME = "UserAuthenticationSessionToken";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationSessionTokenResource>.ResourceManager
  {
    public const string COLUMN_USER_AUTHENTICATION_ID = "UserAuthenticationId";
    public const string COLUMN_EXPIRY_TIME = "ExpiryTime";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.GetManager<UserAuthenticationResource.ResourceManager>().ResourceDeleted += (transaction, resource, cancellationToken) =>
      {
        Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_AUTHENTICATION_ID, "=", resource.Id), cancellationToken);
      };
    }

    protected override UserAuthenticationSessionTokenResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_USER_AUTHENTICATION_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_EXPIRY_TIME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_AUTHENTICATION_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_EXPIRY_TIME} bigint not null;");
      }
    }

    public UserAuthenticationSessionTokenResource Create(ResourceService.Transaction transaction, UserAuthenticationResource userAuthentication, long expireTimer, CancellationToken cancellationToken = default)
    {
      lock (userAuthentication)
      {
        userAuthentication.ThrowIfInvalid();

        if (userAuthentication.Type != UserAuthenticationResource.UserAuthenticationType.SessionToken)
        {
          throw new ArgumentException("Invalid user authentication type.", nameof(userAuthentication));
        }

        return InsertAndGet(transaction, new(
          (COLUMN_USER_AUTHENTICATION_ID, userAuthentication.Id),
          (COLUMN_EXPIRY_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + expireTimer)
        ), cancellationToken);
      }
    }

    public UserAuthenticationSessionTokenResource GetByUserAuthentication(ResourceService.Transaction transaction, UserAuthenticationResource userAuthentication, CancellationToken cancellationToken = default)
    {
      lock (userAuthentication)
      {
        userAuthentication.ThrowIfInvalid();

        if (userAuthentication.Type != UserAuthenticationResource.UserAuthenticationType.SessionToken)
        {
          throw new ArgumentException("Invalid user authentication type.", nameof(userAuthentication));
        }

        return SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_USER_AUTHENTICATION_ID, "=", userAuthentication.Id), cancellationToken: cancellationToken)!;
      }
    }

    public bool ResetExpiryTime(ResourceService.Transaction transaction, UserAuthenticationSessionTokenResource userAuthenticationSessionToken, long expireTimer, CancellationToken cancellationToken = default)
    {
      lock (userAuthenticationSessionToken)
      {
        userAuthenticationSessionToken.ThrowIfInvalid();

        return Update(transaction, userAuthenticationSessionToken, new(
          (COLUMN_EXPIRY_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + expireTimer)
        ), cancellationToken);
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long AuthenticationId,
    long ExpiryTime
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationSessionTokenResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long AuthenticationId => Data.AuthenticationId;
  public long ExpiryTime => Data.ExpiryTime;

  public bool Expired => ExpiryTime <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

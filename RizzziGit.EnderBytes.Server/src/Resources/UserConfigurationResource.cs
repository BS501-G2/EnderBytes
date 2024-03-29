using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserConfigurationResource(UserConfigurationResource.ResourceManager manager, UserConfigurationResource.ResourceData data) : Resource<UserConfigurationResource.ResourceManager, UserConfigurationResource.ResourceData, UserConfigurationResource>(manager, data)
{
  public const string NAME = "UserConfiguration";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserConfigurationResource>.ResourceManager
  {
    public const string COLUMN_USER_ID = "UserId";
    public const string COLUMN_ENABLE_FTP_ACCESS = "EnableFTPAccess";

    public const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.GetResourceManager<UserResource.ResourceManager>().ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id), cancellationToken);
    }

    protected override UserConfigurationResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_USER_ID)),
      reader.GetBoolean(reader.GetOrdinal(COLUMN_ENABLE_FTP_ACCESS))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENABLE_FTP_ACCESS} bigint not null;");

        SqlNonQuery(transaction, $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});");
      }
    }

    public UserConfigurationResource Get(ResourceService.Transaction transaction, UserResource user)
    {
      lock (user)
      {
        user.ThrowIfInvalid();

        return SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id))
          ?? InsertAndGet(transaction, new(
            (COLUMN_USER_ID, user.Id),
            (COLUMN_ENABLE_FTP_ACCESS, false)
          ));
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long UserId,
    bool EnableFtpAccess
  ) : Resource<ResourceManager, ResourceData, UserConfigurationResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long UserId => Data.UserId;
  public bool EnableFtpAccess => Data.EnableFtpAccess;
}

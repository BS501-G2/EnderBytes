using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserConfigurationResource(UserConfigurationResource.ResourceManager manager, UserConfigurationResource.ResourceData data) : Resource<UserConfigurationResource.ResourceManager, UserConfigurationResource.ResourceData, UserConfigurationResource>(manager, data)
{
  private const string NAME = "UserConfiguration";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserConfigurationResource>.ResourceManager
  {
    private const string COLUMN_USER_ID = "UserId";
    private const string COLUMN_ENABLE_FTP_ACCESS = "EnableFTPAccess";

    private const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", resource.Id));
    }

    protected override UserConfigurationResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_USER_ID)),
      reader.GetBoolean(reader.GetOrdinal(COLUMN_ENABLE_FTP_ACCESS))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENABLE_FTP_ACCESS} integer not null;");

        SqlNonQuery(transaction, $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});");
      }
    }

    public UserConfigurationResource Get(ResourceService.Transaction transaction, UserResource user) => SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id))
      ?? Insert(transaction, new(
        (COLUMN_USER_ID, user.Id),
        (COLUMN_ENABLE_FTP_ACCESS, false)
      ));
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
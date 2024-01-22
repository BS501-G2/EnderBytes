using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class UserResource(UserResource.ResourceManager manager, UserResource.ResourceData data) : Resource<UserResource.ResourceManager, UserResource.ResourceData, UserResource>(manager, data)
{
  private const string NAME = "User";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager
  {
    private const string COLUMN_USERNAME = "Username";
    private const string COLUMN_DISPLAY_NAME = "DisplayName";

    private const string INDEX_USERNAME = $"Index_{NAME}_{COLUMN_USERNAME}";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
    }

    protected override UserResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetString(reader.GetOrdinal(COLUMN_USERNAME)),
      reader.GetStringOptional(reader.GetOrdinal(COLUMN_DISPLAY_NAME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_USERNAME} varchar(16) not null;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_DISPLAY_NAME} varchar(32) null;");

        SqlNonQuery(transaction, $"create index {INDEX_USERNAME} on {Name}({COLUMN_USERNAME});");
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, string Username, string? DisplayName) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(Id, CreateTime, UpdateTime);
}

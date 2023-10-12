using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class User(User.ResourceManager manager, User.ResourceData data) : Resource<User.ResourceManager, User.ResourceData, User>(manager, data)
{
  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, string Username) : Resource<ResourceManager, ResourceData, User>.ResourceData(Id, CreateTime, UpdateTime);

  public new sealed class ResourceManager(MainResourceManager main, Database database) : Resource<ResourceManager, ResourceData, User>.ResourceManager(main, database, NAME, VERSION)
  {
    private static readonly Regex ValidUsernameRegex = new("^[A-Za-z0-9_\\-\\.]{6,16}$");

    public const string NAME = "User";
    public const int VERSION = 1;

    private const string KEY_USERNAME = "Username";
    private const string KEY_NAME = "Name";

    private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_USERNAME}";

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (string)reader[KEY_USERNAME]
    );

    protected override User CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_USERNAME} varchar(16) not null collate nocase;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_USERNAME});");
      }
    }
  }
}

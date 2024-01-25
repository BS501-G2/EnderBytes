using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed partial class UserResource(UserResource.ResourceManager manager, UserResource.ResourceData data) : Resource<UserResource.ResourceManager, UserResource.ResourceData, UserResource>(manager, data)
{
  private const string NAME = "User";
  private const int VERSION = 1;

  public new sealed partial class ResourceManager : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager
  {
    private const string COLUMN_USERNAME = "Username";
    private const string COLUMN_DISPLAY_NAME = "DisplayName";

    private const string INDEX_USERNAME = $"Index_{NAME}_{COLUMN_USERNAME}";
    private const string CONSTRAINT_USERNAME_LENGTH = $"Constraint_{COLUMN_USERNAME}";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
    }

    protected override UserResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetString(reader.GetOrdinal(COLUMN_USERNAME)),
      reader.GetStringOptional(reader.GetOrdinal(COLUMN_DISPLAY_NAME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_USERNAME} varchar(16) not null collate nocase;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_DISPLAY_NAME} varchar(32) null;");

        SqlNonQuery(transaction, $"create unique index {INDEX_USERNAME} on {Name}({COLUMN_USERNAME});");
      }
    }

    public UserResource Create(ResourceService.Transaction transaction, string username, string? displayName) => Insert(transaction, new(
      (COLUMN_USERNAME, ThrowIfInvalidUsername(transaction, username)),
      (COLUMN_DISPLAY_NAME, displayName)
    ));

    public bool Update(ResourceService.Transaction transaction, UserResource resource, string username, string? displayName) => Update(transaction, resource, new(
      (COLUMN_USERNAME, ThrowIfInvalidUsername(transaction, username)),
      (COLUMN_DISPLAY_NAME, displayName)
    ));

    public UserResource? GetByUsername(ResourceService.Transaction transaction, string username)
    {
      if (ValidateUsername(username) != UsernameValidationFlag.NoErrors)
      {
        return null;
      }

      return Select(transaction, new WhereClause.CompareColumn(
        COLUMN_USERNAME, "=", username)
      ).FirstOrDefault();
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, string Username, string? DisplayName) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(Id, CreateTime, UpdateTime);

  public string Username => Data.Username;
  public string? DisplayName => Data.DisplayName;
}

using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public enum UserRoleType : byte
{
  Admin, User
}

public sealed class UserRoleResource : Resource<UserRoleResource.ResourceManager, UserRoleResource.ResourceData, UserRoleResource>
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserRoleResource>.ResourceManager
  {
    private const string NAME = "UserRole";
    private const int VERSION = 1;

    private const string KEY_USER_ID = "UserID";
    private const string KEY_TYPE = "Type";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Users.OnResourceDelete((transaction, user, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_USER_ID, ("=", user.Id, null) }
      }, cancellationToken));
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (long)reader[KEY_USER_ID],
      (UserRoleType)(byte)(long)reader[KEY_TYPE]
    );

    protected override UserRoleResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
      }
    }

    public UserRoleType Get(DatabaseTransaction transaction, UserResource user)
    {
      using SqliteDataReader reader = DbSelect(transaction, new()
      {
        { KEY_USER_ID, ("=", user.Id, null) }
      }, [], (1, null), null);

      while (reader.Read())
      {
        return Memory.ResolveFromData(CreateData(reader)).Type;
      }

      return UserRoleType.User;
    }

    public async Task Set(DatabaseTransaction transaction, UserResource user, UserRoleType type, CancellationToken cancellationToken)
    {
      if (await DbUpdate(transaction, new()
      {
        { KEY_TYPE, (byte)type }
      }, new()
      {
        { KEY_USER_ID, ("=", user.Id, null) }
      }, cancellationToken) == 0)
      {
        DbInsert(transaction, new() { { KEY_TYPE, (byte)type } });
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserId,
    UserRoleType Type
  ) : Resource<ResourceManager, ResourceData, UserRoleResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_USER_ID = "userId";
    [JsonPropertyName(KEY_USER_ID)]
    public long UserId = UserId;

    public const string KEY_TYPE = "type";
    [JsonPropertyName(KEY_TYPE)]
    public UserRoleType Type = Type;
  }

  public UserRoleResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public long UserId => Data.UserId;
  public UserRoleType Type => Data.Type;
}

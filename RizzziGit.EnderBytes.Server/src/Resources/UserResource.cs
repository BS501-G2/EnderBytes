using System.Data.SQLite;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class UserResource(UserResource.ResourceManager manager, UserResource.ResourceData data) : Resource<UserResource.ResourceManager, UserResource.ResourceData, UserResource>(manager, data)
{
  public const string NAME = "User";
  public const int VERSION = 1;

  private const string KEY_USERNAME = "Username";

  private const string INDEX_USERNAME = $"Index_{NAME}_{KEY_USERNAME}";

  public const int EXCEPTION_CREATE_RESOURCE_USERNAME_INVALID = 1 << 0;
  public const int EXCEPTION_CREATE_RESOURCE_USERNAME_TAKEN = 1 << 1;

  public const string JSON_KEY_USERNAME = "username";

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    string username
  ) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(id, createTime, updateTime)
  {
    public string Username = username;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      Username = data.Username;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_USERNAME, Username }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager(main, VERSION, NAME)
  {
    private static readonly Regex ValidUsernameRegex = new("^[A-Za-z0-9_\\-\\.]{6,16}$");

    protected override UserResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,

      (string)reader[KEY_USERNAME]
    );

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {Name} add column {KEY_USERNAME} varchar(16) not null collate nocase;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"create unique index {INDEX_USERNAME} on {NAME}({KEY_USERNAME});", cancellationToken);
      }
    }

    public async Task<UserResource> Create(SQLiteConnection connection, string username, CancellationToken cancellationToken)
    {
      if (!ValidUsernameRegex.IsMatch(username))
      {
        throw new ArgumentException("Invalid username.", nameof(username));
      }
      else if (await GetByUsername(connection, username, cancellationToken) != null)
      {
        throw new ArgumentException("Username is taken.", nameof(username));
      }

      return await DbInsert(connection, new() { { KEY_USERNAME, username } }, cancellationToken);
    }

    public Task<UserResource?> GetByUsername(SQLiteConnection connection, string username, CancellationToken cancellationToken) => GetByUsername(connection, username, null, cancellationToken);
    public Task<UserResource?> GetByUsername(SQLiteConnection connection, string username, int? offset, CancellationToken cancellationToken) => DbSelectOne(connection, new() { { KEY_USERNAME, ("=", username) } }, offset, null, cancellationToken);
  }

  public string Username => Data.Username;
}

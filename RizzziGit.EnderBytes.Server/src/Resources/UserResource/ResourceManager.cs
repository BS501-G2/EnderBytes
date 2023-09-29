using System.Data.SQLite;
namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed partial class UserResource
{
  public const string NAME = "User";
  public const int VERSION = 1;

  public const string KEY_USERNAME = "Username";

  public const string INDEX_USERNAME = $"Index_{NAME}_{KEY_USERNAME}";

  public new sealed partial class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager(main, VERSION, NAME)
  {
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

    public Task<UserResource> Create(string username, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => Create(connection, username, cancellationToken), cancellationToken);
    public async Task<UserResource> Create(SQLiteConnection connection, string username, CancellationToken cancellationToken)
    {
      return await DbInsert(connection, new() { { KEY_USERNAME, username } }, cancellationToken);
    }
  }
}

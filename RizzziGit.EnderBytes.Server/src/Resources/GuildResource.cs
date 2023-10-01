using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class GuildResource(GuildResource.ResourceManager manager, GuildResource.ResourceData data) : Resource<GuildResource.ResourceManager, GuildResource.ResourceData, GuildResource>(manager, data)
{
  public const string NAME = "Guild";
  public const int VERSION = 1;

  private const string KEY_OWNER_USER_ID = "OwnerUserID";
  private const string KEY_NAME = "Name";
  private const string KEY_DESCRIPTION = "Description";

  private const string INDEX_OWNER_USER_ID = $"Index_{NAME}_{KEY_OWNER_USER_ID}";

  public const string JSON_KEY_OWNER_USER_ID = "ownerUserId";
  public const string JSON_KEY_NAME = "name";
  public const string JSON_KEY_DESCRIPTION = "description";

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    ulong ownerUserId,
    string name,
    string? description
  ) : Resource<ResourceManager, ResourceData, GuildResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong OwnerUserID = ownerUserId;
    public string Name = name;
    public string? Description = description;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      OwnerUserID = data.OwnerUserID;
      Name = data.Name;
      Description = data.Description;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_OWNER_USER_ID, OwnerUserID },
        { JSON_KEY_NAME, Name },
        { JSON_KEY_DESCRIPTION, Description }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, GuildResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main) : base(main, VERSION, NAME)
    {
      main.Users.ResourceDeleteHandles.Add(DeleteAllFromUser);
    }

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,

      (ulong)(long)reader[KEY_OWNER_USER_ID],
      (string)reader[KEY_NAME],
      reader[KEY_DESCRIPTION] is not DBNull ? (string)reader[KEY_DESCRIPTION] : null
    );

    protected override GuildResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NAME} varchar(64) not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_DESCRIPTION} varchar(4096);", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create unique index {INDEX_OWNER_USER_ID} on {NAME}({KEY_OWNER_USER_ID});", cancellationToken);
      }
    }

    public Task<ResourceStream> Stream(SQLiteConnection connection, UserResource owner, int? offset, int? length, CancellationToken cancellationToken) => Wrapper.Select(connection, new()
    {
      { KEY_OWNER_USER_ID, ("=", owner.ID) }
    }, offset, length, cancellationToken);

    public Task<bool> DeleteAllFromUser(SQLiteConnection connection, UserResource owner, CancellationToken cancellationToken) => DbDelete(connection, new()
    {
      { KEY_OWNER_USER_ID, ("=", owner.ID) }
    }, cancellationToken);

    public Task<GuildResource> Create(SQLiteConnection connection, UserResource owner, string name, string? description, CancellationToken cancellationToken) => DbInsert(connection, new()
    {
      { KEY_OWNER_USER_ID, owner.ID },
      { KEY_NAME, name },
      { KEY_DESCRIPTION, description }
    }, cancellationToken);
  }
}

using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Newtonsoft.Json.Linq;

public sealed class StoragePoolResource(StoragePoolResource.ResourceManager manager, StoragePoolResource.ResourceData data) : Resource<StoragePoolResource.ResourceManager, StoragePoolResource.ResourceData, StoragePoolResource>(manager, data)
{
  public const string NAME = "StoragePool";
  public const int VERSION = 1;

  private const string KEY_NAME = "Name";
  private const string KEY_TYPE = "Type";
  private const string KEY_FLAGS = "Flags";
  private const string KEY_OWNER_USER_ID = "OwnerUserID";
  private const string KEY_PAYLOAD = "Payload";

  private const string KEY_PAYLOAD_KEY_LOCATION = "location";
  private const string KEY_PAYLOAD_KEY_DIFFERENTIATOR = "differentiator";
  private const string KEY_PAYLOAD_KEY_USERNAME = "username";
  private const string KEY_PAYLOAD_KEY_PASSWORD = "password";

  public const string JSON_KEY_NAME = "name";
  public const string JSON_KEY_TYPE = "type";
  public const string JSON_KEY_FLAGS = "flags";
  public const string JSON_KEY_OWNER_USER_ID = "ownerUserId";
  public const string JSON_KEY_PAYLOAD = "payload";

  public const uint TYPE_VIRTUAL_POOL = 0;
  public const uint TYPE_PHYSICAL_POOL = 1;
  public const uint TYPE_REMOTE_POOL = 2;

  public const byte FLAG_SESSION_TEMPORAL = 1 << 0;
  public const byte FLAG_USER_TEMPORAL = 1 << 1;
  public const byte FLAG_IGNORE_CASE = 1 << 2;

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    string name,
    uint type,
    byte flags,
    ulong ownerUserId,
    string payload
  ) : Resource<ResourceManager, ResourceData, StoragePoolResource>.ResourceData(id, createTime, updateTime)
  {
    public string Name = name;
    public uint Type = type;
    public byte Flags = flags;
    public ulong OwnerUserID = ownerUserId;
    public string Payload = payload;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      Name = data.Name;
      Type = data.Type;
      Flags = data.Flags;
      OwnerUserID = data.OwnerUserID;
      Payload = data.Payload;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_NAME, Name },
        { JSON_KEY_TYPE, Type },
        { JSON_KEY_FLAGS, Flags },
        { JSON_KEY_OWNER_USER_ID, OwnerUserID },
        { JSON_KEY_PAYLOAD, Payload }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, StoragePoolResource>.ResourceManager(main, VERSION, NAME)
  {
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,

      (string)reader[KEY_NAME],
      (uint)(long)reader[KEY_TYPE],
      (byte)(long)reader[KEY_FLAGS],
      (ulong)(long)reader[KEY_OWNER_USER_ID],
      (string)reader[KEY_PAYLOAD]
    );

    protected override StoragePoolResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NAME} varchar(64) not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_TYPE} integer not null", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_FLAGS} integer not null", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null", cancellationToken);
      }
    }

    private Task<StoragePoolResource> Create(SQLiteConnection connection, string name, uint type, byte flags, JObject payload, CancellationToken cancellationToken) => DbInsert(connection, new()
    {
      { KEY_NAME, name },
      { KEY_TYPE, type },
      { KEY_FLAGS, flags },
      { KEY_PAYLOAD, payload.ToString() },
    }, cancellationToken);

    public Task<StoragePoolResource> CreatePhysicalPool(SQLiteConnection connection, string name, string location, byte flags, CancellationToken cancellationToken) => Create(
      connection, name, TYPE_PHYSICAL_POOL, flags,
      new JObject()
      {
        { KEY_PAYLOAD_KEY_LOCATION, location }
      },
      cancellationToken
    );

    public Task<StoragePoolResource> CreateSharedPool(SQLiteConnection connection, string name, byte flags, CancellationToken cancellationToken) => Create(
      connection, name, TYPE_VIRTUAL_POOL, flags, [], cancellationToken
    );

    public Task<StoragePoolResource> CreateNestedRemotePool(SQLiteConnection connection, string name, string location, string username, string password, byte flags, CancellationToken cancellationToken) => Create(
      connection, name, TYPE_REMOTE_POOL, flags,
      new JObject()
      {
        { KEY_PAYLOAD_KEY_LOCATION, location },
        { KEY_PAYLOAD_KEY_USERNAME, username },
        { KEY_PAYLOAD_KEY_PASSWORD, password }
      },
      cancellationToken
    );
  }

  public string Name => Data.Name;
  public uint Type => Data.Type;
  public byte Flags => Data.Flags;
  public ulong OwnerUserID => Data.OwnerUserID;
}

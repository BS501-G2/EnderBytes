using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Buffer;
using Database;
using Connections;

public enum StoragePoolType : byte
{
  Blob,
  Physical,
  Remote
}

[Flags]
public enum StoragePoolFlags : byte
{
  IgnoreCase = 1 << 0,
  ConnectionIsolated = 1 << 1,
  SessionIsolated = 1 << 2,
  Internal = 1 << 3
}

public sealed class StoragePoolResource(StoragePoolResource.ResourceManager manager, StoragePoolResource.ResourceData data) : Resource<StoragePoolResource.ResourceManager, StoragePoolResource.ResourceData, StoragePoolResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, StoragePoolResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Users.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new() { { KEY_USER_ID, ("=", resource.Id, null) } });
    }

    public const string NAME = "StoragePool";
    public const int VERSION = 1;

    private const string KEY_USER_ID = "UserID";
    private const string KEY_TYPE = "Type";
    private const string KEY_FLAGS = "Flags";
    private const string KEY_PAYLOAD = "Payload";

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader[KEY_USER_ID] is DBNull ? null : (long)reader[KEY_USER_ID],
      (byte)(long)reader[KEY_TYPE],
      (byte)(long)reader[KEY_FLAGS],
      JObject.Parse(Encoding.UTF8.GetString((byte[])reader[KEY_PAYLOAD]))
    );

    protected override StoragePoolResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_ID};");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FLAGS} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PAYLOAD} blob not null;");
      }
    }

    public StoragePoolResource CreateBlob(DatabaseTransaction transaction, long userId, StoragePoolFlags flags) => DbInsert(transaction, new()
    {
      { KEY_USER_ID, userId },
      { KEY_TYPE, (byte)StoragePoolType.Blob },
      { KEY_FLAGS, (byte)flags },
      { KEY_PAYLOAD, Encoding.UTF8.GetBytes("{}") }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? UserId,
    byte Type,
    byte Flags,
    JObject Payload
  ) : Resource<ResourceManager, ResourceData, StoragePoolResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? UserId => Data.UserId;
  public StoragePoolType Type => (StoragePoolType)Data.Type;
  public StoragePoolFlags Flags => (StoragePoolFlags)Data.Flags;
  public JObject Payload => Payload;
}

using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

[Flags]
public enum HubFlags
{
  PersonalMain = 1 << 0
}

public sealed class HubResource(HubResource.ResourceManager manager, HubResource.ResourceData data) : Resource<HubResource.ResourceManager, HubResource.ResourceData, HubResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, HubResource>.ResourceManager
  {
    public const string NAME = "Hub";
    public const int VERSION = 1;

    private const string KEY_OWNER_USER_ID = "OwnerUserId";
    private const string KEY_KEY_SHARED_ID = "KeySharedId";
    private const string KEY_FLAGS = "Flags";

    private const string INDEX_UNIQUENESS = $"Index_{KEY_KEY_SHARED_ID}";

    public ResourceManager(Resources.ResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Users.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new() { { KEY_OWNER_USER_ID, ("=", resource.Id) } });
    }

    protected override HubResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_OWNER_USER_ID],
      (long)reader[KEY_KEY_SHARED_ID],
      (HubFlags)(byte)(long)reader[KEY_FLAGS]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_KEY_SHARED_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FLAGS} integer not null;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_OWNER_USER_ID},{KEY_KEY_SHARED_ID});");
      }
    }

    public HubResource Create(
      DatabaseTransaction transaction,
      UserResource owner,
      KeyResource key,
      HubFlags flags
    ) => DbInsert(transaction, new()
    {
      { KEY_OWNER_USER_ID, owner.Id },
      { KEY_KEY_SHARED_ID, key.SharedId },
      { KEY_FLAGS, (byte)flags }
    });

    public HubResource? GetMainByUserId(
      DatabaseTransaction transaction,
      UserResource owner
    )
    {
      foreach (HubResource hub in DbStream(transaction, new()
      {
        { KEY_OWNER_USER_ID, ("=", owner.Id) }
      }))
      {
        if (hub.Flags.HasFlag(HubFlags.PersonalMain))
        {
          return hub;
        }
      }

      return null;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long OwnerUserId,
    long KeySharedId,
    HubFlags Flags
  ) : Resource<ResourceManager, ResourceData, HubResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long OwnerUserId => Data.OwnerUserId;
  public long KeySharedId => Data.KeySharedId;
  public HubFlags Flags => Data.Flags;
}

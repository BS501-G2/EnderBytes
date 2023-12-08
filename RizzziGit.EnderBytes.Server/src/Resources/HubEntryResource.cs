using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using StoragePools;

public sealed class HubEntryResource(HubEntryResource.ResourceManager manager, HubEntryResource.ResourceData data) : Resource<HubEntryResource.ResourceManager, HubEntryResource.ResourceData, HubEntryResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, HubEntryResource>.ResourceManager
  {
    public const string NAME = "Hub";
    public const int VERSION = 1;

    private const string KEY_PERSONAL_HUB_ID = "HubId";
    private const string KEY_STORAGE_POOL_ID = "PoolId";
    private const string KEY_PATH = "Path";

    private const string INDEX_UNIQUENESS = $"Index_{KEY_PATH}_{KEY_PERSONAL_HUB_ID}";

    public ResourceManager(Resources.ResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Hubs.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new() { { KEY_PERSONAL_HUB_ID, ("=", resource.Id) } });
    }

    protected override HubEntryResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_PERSONAL_HUB_ID],
      (long)reader[KEY_STORAGE_POOL_ID],

      new StoragePool.Path()
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PERSONAL_HUB_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PATH} blob not null;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_PATH},{KEY_PERSONAL_HUB_ID})");
      }
    }

    public HubEntryResource Create(
      DatabaseTransaction transaction,
      HubResource personalHub,
      StoragePoolResource storagePool,
      StoragePool.Path pathPrefix
    )
    {
      return DbInsert(transaction, new()
      {
        { KEY_PERSONAL_HUB_ID, personalHub.Id },
        { KEY_STORAGE_POOL_ID, storagePool.Id },
        { KEY_PATH, pathPrefix.Serialize() }
      });
    }

    public IEnumerable<HubEntryResource> Stream(
      DatabaseTransaction transaction,
      HubResource personalHub
    ) => DbStream(transaction, new()
    {
      { KEY_PERSONAL_HUB_ID, ("=", personalHub.Id) }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long PersonalHubId,
    long StoragePoolId,
    StoragePool.Path Path
  ) : Resource<ResourceManager, ResourceData, HubEntryResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long PersonalHubId => Data.PersonalHubId;
  public long StoragePoolId => Data.StoragePoolId;
  public StoragePool.Path Path => Data.Path;
}

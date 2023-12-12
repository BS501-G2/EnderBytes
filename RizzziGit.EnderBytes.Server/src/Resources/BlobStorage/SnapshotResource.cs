using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobSnapshotResource(BlobSnapshotResource.ResourceManager manager, BlobSnapshotResource.ResourceData data) : Resource<BlobSnapshotResource.ResourceManager, BlobSnapshotResource.ResourceData, BlobSnapshotResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobSnapshotResource>.ResourceManager
  {
    public const string NAME = "BlobSnapshot";
    public const int VERSION = 1;

    private const string KEY_NODE_ID = "NodeId";
    private const string KEY_BASE_SNAPSHOT_ID = "BaseSnapshotId";
    private const string KEY_SIZE = "Size";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobSnapshotResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_BASE_SNAPSHOT_ID],
      (long)reader[KEY_SIZE]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NODE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BASE_SNAPSHOT_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_SIZE} integer not null;");
      }
    }

    public BlobSnapshotResource Create(DatabaseTransaction transaction, BlobNodeResource node, BlobSnapshotResource? baseSnapshot, long size = 0) => DbInsert(transaction, new()
    {
      { KEY_NODE_ID, node.Id },
      { KEY_BASE_SNAPSHOT_ID, baseSnapshot?.Id },
      { KEY_SIZE, size }
    });

    public IEnumerable<BlobSnapshotResource> Stream(DatabaseTransaction transaction, BlobNodeResource node) => DbStream(transaction, new()
    {
      { KEY_NODE_ID, ("=", node.Id) }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long BaseSnapshotId,
    long Size
  ) : Resource<ResourceManager, ResourceData, BlobSnapshotResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long BaseSnapshotId => Data.BaseSnapshotId;
  public long Size => Data.Size;
}

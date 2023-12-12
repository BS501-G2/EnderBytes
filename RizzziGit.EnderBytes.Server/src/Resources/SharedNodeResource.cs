using Microsoft.Data.Sqlite;
namespace RizzziGit.EnderBytes.Resources;

using Database;

[Flags]
public enum SharedNodeAccess : byte
{
  Read = 1 << 0,
  Write = 1 << 1,

  ReadWrite = Read | Write
}

public sealed class SharedNodeResource(SharedNodeResource.ResourceManager manager, SharedNodeResource.ResourceData data) : Resource<SharedNodeResource.ResourceManager, SharedNodeResource.ResourceData, SharedNodeResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, SharedNodeResource>.ResourceManager
  {
    public const string NAME = "SharedNode";
    public const int VERSION = 1;

    private const string KEY_AUTHOR_USER_ID = "AuthorUserId";
    private const string KEY_STORAGE_KEY_SHARED_ID = "StoragePoolKeySharedId";
    private const string KEY_STORAGE_POOL_ID = "StoragePoolId";
    private const string KEY_NODE_KEY_SHARED_ID = "NodeKeySharedId";
    private const string KEY_NODE_ID = "NodeId";
    private const string KEY_ACCESS = "Access";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override SharedNodeResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_AUTHOR_USER_ID],
      (long)reader[KEY_STORAGE_KEY_SHARED_ID],
      (long)reader[KEY_STORAGE_POOL_ID],
      (long)reader[KEY_NODE_KEY_SHARED_ID],
      (long)reader[KEY_NODE_ID],
      (SharedNodeAccess)(byte)(long)reader[KEY_ACCESS]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHOR_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_KEY_SHARED_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NODE_KEY_SHARED_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NODE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS} integer not null;");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long AuthorUserId,
    long StoragePoolKeySharedId,
    long StoragePoolId,
    long NodeKeySharedId,
    long NodeId,
    SharedNodeAccess Access
  ) : Resource<ResourceManager, ResourceData, SharedNodeResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long AuthorUserId => Data.AuthorUserId;
  public long StoragePoolKeySharedId => Data.StoragePoolKeySharedId;
  public long StoragePoolId => Data.StoragePoolId;
  public long NodeKeySharedId => Data.NodeKeySharedId;
  public long NodeId => Data.NodeId;
  public SharedNodeAccess Access => Data.Access;
}

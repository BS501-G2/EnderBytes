using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;
using Connections;

public enum BlobNodeType : byte { File, Folder, SymbolicLink }
public sealed class BlobNodeResource(BlobNodeResource.ResourceManager manager, BlobNodeResource.ResourceData data) : Resource<BlobNodeResource.ResourceManager, BlobNodeResource.ResourceData, BlobNodeResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobNodeResource>.ResourceManager
  {
    public const string NAME = "BlobNode";
    public const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_NAME = "Name";
    private const string KEY_PARENT_ID = "ParentId";
    private const string KEY_KEY_SHARED_ID = "KeySharedId";
    private const string KEY_TYPE = "Type";

    public ResourceManager(BlobStorage.ResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      Main = main;
    }

    public new readonly BlobStorage.ResourceManager Main;

    protected override BlobNodeResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_ACCESS_TIME],
      (string)reader[KEY_NAME],
      reader[KEY_PARENT_ID] is DBNull ? null : (long)reader[KEY_PARENT_ID],
      (long)reader[KEY_KEY_SHARED_ID],
      (BlobNodeType)(byte)(long)reader[KEY_TYPE]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PARENT_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_KEY_SHARED_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
      }
    }

    public BlobNodeResource? GetByName(DatabaseTransaction transaction, string name, BlobNodeResource? folder) => DbOnce(transaction, new()
    {
      { KEY_NAME, ("=", name) },
      { KEY_PARENT_ID, ("=", folder?.Id) }
    });

    public BlobNodeResource Create(
      DatabaseTransaction transaction,
      string name,
      BlobNodeType type,
      BlobNodeResource? folder,
      KeyResource.Transformer transformer
    ) => DbInsert(transaction, new()
    {
      { KEY_ACCESS_TIME, 0 },
      { KEY_NAME, name },
      { KEY_PARENT_ID, folder?.ParentId },
      { KEY_KEY_SHARED_ID, transformer.Key.SharedId },
      { KEY_TYPE, type }
    });

    public IEnumerable<BlobNodeResource> StreamChildren(
      DatabaseTransaction transaction,
      BlobNodeResource? folder
    ) => DbStream(transaction, new()
    {
      { KEY_PARENT_ID, ("=", folder?.Id) }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long AccessTime,
    string Name,
    long? ParentId,
    long KeySharedId,
    BlobNodeType Type
  ) : Resource<ResourceManager, ResourceData, BlobNodeResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long AccessTime => Data.AccessTime;
  public string Name => Data.Name;
  public long? ParentId => Data.ParentId;
  public long KeySharedId => Data.KeySharedId;
  public BlobNodeType Type => Data.Type;
}

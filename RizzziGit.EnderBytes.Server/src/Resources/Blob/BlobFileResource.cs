using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public enum BlobFileType : byte
{
  File,
  Folder,
  SymbolicLink
}

public sealed class BlobFileResource(BlobFileResource.ResourceManager manager, BlobFileResource.ResourceData data) : Resource<BlobFileResource.ResourceManager, BlobFileResource.ResourceData, BlobFileResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceManager
  {
    private const string NAME = "BlobFile";
    private const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_TYPE = "Type";
    private const string KEY_PARENT_ID = "ParentId";
    private const string KEY_NAME = "Name";
    private const string KEY_PAYLOAD = "Payload";

    public ResourceManager(BlobStorageResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      ResourceDeleted += (transaction, resource) => DbDelete(transaction, new() { { KEY_PARENT_ID, ("=", resource.Id) } });
    }

    public new BlobStorageResourceManager Main => (BlobStorageResourceManager)base.Main;

    protected override BlobFileResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader[KEY_ACCESS_TIME] is DBNull ? null : (long)reader[KEY_ACCESS_TIME],
      reader[KEY_TRASH_TIME] is DBNull ? null : (long)reader[KEY_TRASH_TIME],
      (BlobFileType)(byte)(long)reader[KEY_TYPE],
      reader[KEY_PARENT_ID] is DBNull ? null : (long)reader[KEY_PARENT_ID],
      (string)reader[KEY_NAME],
      reader[KEY_PAYLOAD] is DBNull ? null : (byte[])reader[KEY_PAYLOAD]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TRASH_TIME} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PARENT_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null{(Main.StoragePool.Resource.Flags.HasFlag(StoragePoolFlags.IgnoreCase) ? $" collate nocase" : "")};");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PAYLOAD} blob;");
      }
    }

    public IEnumerable<BlobFileResource> Stream(DatabaseTransaction transaction, BlobFileResource? parentFolder, Limit? limit = null, List<Order>? order = null) => DbStream(transaction, new()
    {
      { KEY_PARENT_ID, ("=", parentFolder?.Id) },
      { KEY_TRASH_TIME, ("=", null) }
    }, limit, order);

    public IEnumerable<BlobFileResource> StreamTrash(DatabaseTransaction transaction, Limit? limit = null, List<Order>? order = null) => DbStream(transaction, new()
    {
      { KEY_TRASH_TIME, ("!=", null) }
    }, limit, order);

    public BlobFileResource? GetByName(DatabaseTransaction transaction, BlobFileResource? parentFolder, string name)
    {
      foreach (BlobFileResource file in DbStream(transaction, new()
      {
        { KEY_PARENT_ID, ("=", parentFolder?.Id) },
        { KEY_NAME, ("=", name) }
      }, new(1, null)))
      {
        return file;
      }

      return null;
    }

    public BlobFileResource CreateFolder(DatabaseTransaction transaction, BlobFileResource? parentFolder, string name) => DbInsert(transaction, new()
    {
      { KEY_ACCESS_TIME, null },
      { KEY_TRASH_TIME, null },
      { KEY_TYPE, (byte)BlobFileType.Folder },
      { KEY_PARENT_ID, parentFolder?.Id },
      { KEY_NAME, name },
      { KEY_PAYLOAD, null }
    });

    public BlobFileResource CreateFile(DatabaseTransaction transaction, BlobFileResource? parentFolder, string name) => DbInsert(transaction, new()
    {
      { KEY_ACCESS_TIME, null },
      { KEY_TRASH_TIME, null },
      { KEY_TYPE, (byte)BlobFileType.File },
      { KEY_PARENT_ID, parentFolder?.Id },
      { KEY_NAME, name },
      { KEY_PAYLOAD, null }
    });

    public BlobFileResource CreateSymbolicLink(DatabaseTransaction transaction, BlobFileResource? parentFolder, string name) => DbInsert(transaction, new()
    {
      { KEY_ACCESS_TIME, null },
      { KEY_TRASH_TIME, null },
      { KEY_TYPE, (byte)BlobFileType.SymbolicLink },
      { KEY_PARENT_ID, parentFolder?.Id },
      { KEY_NAME, name },
      { KEY_PAYLOAD, null }
    });

    public bool Trash(DatabaseTransaction transaction, BlobFileResource file)
    {
      if (file.TrashTime != null)
      {
        return false;
      }

      return DbUpdate(transaction, new()
      {
        { KEY_TRASH_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
      }, new()
      {
        { KEY_ID, ("=", file.Id) }
      }) != 0;
    }

    public bool Restore(DatabaseTransaction transaction, BlobFileResource file, BlobFileResource? parentFolder)
    {
      if (file.TrashTime == null)
      {
        return false;
      }

      return DbUpdate(transaction, new()
      {
        { KEY_TRASH_TIME, null },
        { KEY_PARENT_ID, parentFolder?.Id }
      }, new()
      {
        { KEY_ID, ("=", file.Id) }
      }) != 0;
    }

    public bool Move(DatabaseTransaction transaction, BlobFileResource file, BlobFileResource? parentFolder)
    {
      if (file.ParentId == parentFolder?.Id)
      {
        return false;
      }

      return DbUpdate(transaction, new()
      {
        { KEY_PARENT_ID, parentFolder?.Id }
      }, new()
      {
        { KEY_ID, ("=", file.Id) }
      }) != 0;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? AccessTime,
    long? TrashTime,
    BlobFileType Type,
    long? ParentId,
    string Name,
    byte[]? Payload
  ) : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? AccessTime => Data.AccessTime;
  public long? TrashTime => Data.TrashTime;
  public BlobFileType Type => Data.Type;
  public long? ParentId => Data.ParentId;
  public string Name => Data.Name;
  public byte[]? Payload => Data.Payload;
}

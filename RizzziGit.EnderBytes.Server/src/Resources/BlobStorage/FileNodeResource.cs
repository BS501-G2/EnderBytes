using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public enum FileNodeType { File, Directory, SymbolicLink }

public sealed class FileNodeResource(FileNodeResource.ResourceManager manager, FileNodeResource.ResourceData data) : Resource<FileNodeResource.ResourceManager, FileNodeResource.ResourceData, FileNodeResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceManager
  {
    private const string NAME = "FileSystem";
    private const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_TRASH_TIME = "TrashTime";
    private const string KEY_PARENT_ID = "ParentNode";
    private const string KEY_TYPE = "Type";
    private const string KEY_NAME = "Name";

    public ResourceManager(BlobStorage.ResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      Main = main;
    }

    public new readonly BlobStorage.ResourceManager Main;

    protected override FileNodeResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader[KEY_ACCESS_TIME] is DBNull ? null : (long)reader[KEY_ACCESS_TIME],
      reader[KEY_TRASH_TIME] is DBNull ? null : (long)reader[KEY_TRASH_TIME],
      reader[KEY_PARENT_ID] is DBNull ? null : (long)reader[KEY_PARENT_ID],
      (FileNodeType)(byte)reader[KEY_TYPE],
      (string)reader[KEY_NAME]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TRASH_TIME} integer null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PARENT_ID} integer null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null{(Main.StoragePool.Resource.Flags.HasFlag(StoragePoolFlags.IgnoreCase) ? " collate nocase" : "")};");
      }
    }

    public FileNodeResource? GetByName(DatabaseTransaction transaction, string name, FileNodeResource? parentNode)
    {
      foreach (FileNodeResource node in DbStream(transaction, new()
      {
        { KEY_PARENT_ID, ("=", parentNode?.Id) },
        { KEY_NAME, ("=", name) }
      }, new(1)))
      {
        return node;
      }

      return null;
    }

    public IEnumerable<FileNodeResource> StreamChildrenNodes(DatabaseTransaction transaction, FileNodeResource? parentNode)
    {
      foreach (FileNodeResource node in DbStream(transaction, new()
      {
        { KEY_PARENT_ID, ("=", parentNode?.Id) }
      }))
      {
        yield return node;
      }
    }

    public FileNodeResource CreateFolder(DatabaseTransaction transaction, string name, FileNodeResource? parentNode)
    {
      return DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, null },
        { KEY_TRASH_TIME, null },
        { KEY_PARENT_ID, parentNode?.Id },
        { KEY_TYPE, (byte)FileNodeType.Directory },
        { KEY_NAME, name }
      });
    }

    public FileNodeResource CreateFile(DatabaseTransaction transaction, string name, FileNodeResource parentNode)
    {
      return DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, null },
        { KEY_TRASH_TIME, null },
        { KEY_PARENT_ID, parentNode.Id },
        { KEY_TYPE, (byte)FileNodeType.File },
        { KEY_NAME, name }
      });
    }

    public FileNodeResource CreateSymbolicLink(DatabaseTransaction transaction, string name, FileNodeResource parentNode)
    {
      return DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, null },
        { KEY_TRASH_TIME, null },
        { KEY_PARENT_ID, parentNode.Id },
        { KEY_TYPE, (byte)FileNodeType.SymbolicLink },
        { KEY_NAME, name }
      });
    }

    public bool UpdateParentFolder(DatabaseTransaction transaction, FileNodeResource node, FileNodeResource? parentNode)
    {
      if ((parentNode != null) && (parentNode?.Type == FileNodeType.Directory))
      {
        throw new InvalidOperationException("Invalid node type.");
      }

      return DbUpdate(transaction, new()
      {
        { KEY_PARENT_ID, parentNode?.Id }
      }, new()
      {
        { KEY_ID, ("=", node.Id) }
      }) != 0;
    }

    public bool UpdateTimestamps(DatabaseTransaction transaction, FileNodeResource node, long? accessTime, long? trashTime)
    {
      return DbUpdate(transaction, new()
      {
        { KEY_ACCESS_TIME, accessTime },
        { KEY_TRASH_TIME, trashTime }
      }, new()
      {
        { KEY_ID, ("=", node.Id) }
      }) != 0;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? AccessTime,
    long? TrashTime,
    long? ParentId,
    FileNodeType Type,
    string Name
  ) : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? AccessTime => Data.AccessTime;
  public long? TrashTime => Data.TrashTime;
  public long? ParentId => Data.ParentId;
  public FileNodeType Type => Data.Type;
  public string Name => Data.Name;
}

using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;

public sealed class FileResource(FileResource.ResourceManager manager, StorageResource storage, FileResource? parent, FileResource.ResourceData data) : Resource<FileResource.ResourceManager, FileResource.ResourceData, FileResource>(manager, data)
{
  public enum FileType : byte { File, Folder, SymbolicLink }

  private const string NAME = "File";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileResource>.ResourceManager
  {
    private const string COLUMN_STORAGE_ID = "StorageId";
    private const string COLUMN_KEY = "Key";
    private const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    private const string COLUMN_NAME = "Name";

    private const string UNIQUE_INDEX_NAME = $"Index_{NAME}_{COLUMN_NAME}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Storages.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", resource.Id));
    }

    protected override FileResource NewResource(ResourceService.Transaction transaction, ResourceData data, CancellationToken cancellationToken = default) => new(
      this,
      Service.Storages.GetById(transaction, data.StorageId, cancellationToken),
      data.ParentId != null ? Service.Files.GetById(transaction, (long)data.ParentId, cancellationToken) : null,
      data
    );

    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_STORAGE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_FILE_ID)),
      reader.GetString(reader.GetOrdinal(COLUMN_NAME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_STORAGE_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_FILE_ID} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null;");

        SqlNonQuery(transaction, $"create unique index {UNIQUE_INDEX_NAME} on {NAME}({COLUMN_STORAGE_ID},{COLUMN_PARENT_FILE_ID},{COLUMN_NAME});");
      }
    }

    public FileResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, FileType type, string name, UserAuthenticationResource.UserAuthenticationToken token, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();

          if (parent == null)
          {
            return create();
          }

          lock (parent)
          {
            parent.ThrowIfInvalid();
            return create();
          }
        }
      }

      FileResource create()
      {
        if (!Enum.IsDefined(type))
        {
          throw new ArgumentException("Invalid file type.", nameof(type));
        }
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, string Name) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public readonly StorageResource Storage = storage;
  public FileResource? Parent { get; private set; } = parent;

  public long StorageId => Data.StorageId;
  public byte[] Key => Data.Key;
  public long? ParentId => Data.ParentId;
  public string Name => Data.Name;
}

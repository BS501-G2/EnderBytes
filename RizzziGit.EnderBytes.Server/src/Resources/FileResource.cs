using System.Data.Common;

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
    private const string COLUMN_KEY = "AesKey";
    private const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    private const string COLUMN_TYPE = "Type";
    private const string COLUMN_NAME = "Name";

    private const string UNIQUE_INDEX_NAME = $"Index_{NAME}_{COLUMN_NAME}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Storages.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", resource.Id));
      ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", resource.Id));
    }

    protected override FileResource NewResource(ResourceService.Transaction transaction, ResourceData data, CancellationToken cancellationToken = default) => new(
      this,
      Service.Storages.GetById(transaction, data.StorageId, cancellationToken),
      data.ParentId != null ? Service.Files.GetById(transaction, (long)data.ParentId, cancellationToken) : null,
      data
    );

    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_STORAGE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_FILE_ID)),
      (FileType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE)),
      reader.GetString(reader.GetOrdinal(COLUMN_NAME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_STORAGE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_FILE_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null;");

        SqlNonQuery(transaction, $"create unique index {UNIQUE_INDEX_NAME} on {NAME}({COLUMN_STORAGE_ID},{COLUMN_PARENT_FILE_ID},{COLUMN_NAME});");
      }
    }

    public bool MoveParent(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileResource? newParent, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();

          if (newParent == null)
          {
            return moveParent();
          }

          lock (newParent)
          {
            return moveParent();
          }
        }
      }

      bool moveParent()
      {
        cancellationToken.ThrowIfCancellationRequested();

        if ((newParent != null) && (newParent.Type != FileType.Folder))
        {
          throw new ArgumentException("Invalid new parent.", nameof(parent));
        }

        KeyService.AesPair fileKey = storage.DecryptFileKey(file, userAuthenticationToken);

        return Update(transaction, file, new(
          (COLUMN_PARENT_FILE_ID, newParent?.Id),
          (COLUMN_KEY, newParent != null ? storage.DecryptFileKey(newParent, userAuthenticationToken).Encrypt(fileKey.Serialize()) : storage.EncryptFileKey(fileKey, userAuthenticationToken))
        ), cancellationToken);
      }
    }

    public FileResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, FileType type, string name, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
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
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(type))
        {
          throw new ArgumentException("Invalid file type.", nameof(type));
        }
        else if ((parent != null) && (parent.Type != FileType.Folder))
        {
          throw new ArgumentException("Invalid parent.", nameof(parent));
        }

        KeyService.AesPair fileKey = Service.Server.KeyService.GetNewAesPair();

        return Insert(transaction, new(
          (COLUMN_STORAGE_ID, storage.Id),
          (COLUMN_KEY, parent != null ? storage.DecryptFileKey(parent, userAuthenticationToken).Encrypt(fileKey.Serialize()) : storage.EncryptFileKey(fileKey, userAuthenticationToken)),
          (COLUMN_PARENT_FILE_ID, parent?.Id),
          (COLUMN_NAME, name),
          (COLUMN_TYPE, (byte)type)
        ), cancellationToken);
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, FileType Type, string Name) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public readonly StorageResource Storage = storage;
  public FileResource? Parent { get; private set; } = parent;

  public long StorageId => Data.StorageId;
  public byte[] Key => Data.Key;
  public long? ParentId => Data.ParentId;
  public FileType Type => Data.Type;
  public string Name => Data.Name;
}

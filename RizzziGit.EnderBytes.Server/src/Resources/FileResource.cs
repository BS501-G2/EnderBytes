using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;

public sealed class FileResource(FileResource.ResourceManager manager, FileResource.ResourceData data) : Resource<FileResource.ResourceManager, FileResource.ResourceData, FileResource>(manager, data)
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

    protected override FileResource NewResource(ResourceData data) => new(this, data);

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

    public bool Move(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileResource? newParent, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();
          file.ThrowIfDoesNotBelongTo(storage);

          if (newParent == null)
          {
            return moveParent();
          }

          lock (newParent)
          {
            newParent.ThrowIfDoesNotBelongTo(storage);
            newParent.ThrowIfInvalid();

            FileResource currentParent = newParent;
            while (currentParent != null)
            {
              if (currentParent.Id == file.Id)
              {
                throw new ArgumentException("Moving the file inside the new parent folder closes the loop.", nameof(newParent));
              }
              else if (currentParent.ParentId == null)
              {
                break;
              }

              currentParent = GetById(transaction, (long)currentParent.ParentId, cancellationToken);
            }

            return moveParent();
          }
        }
      }

      bool moveParent()
      {
        cancellationToken.ThrowIfCancellationRequested();

        if ((newParent != null) && (newParent.Type != FileType.Folder))
        {
          throw new ArgumentException("Invalid new parent.", nameof(newParent));
        }

        bool result = Update(transaction, file, new(
          (COLUMN_PARENT_FILE_ID, newParent?.Id),
          (COLUMN_KEY, Service.Storages.EncryptFileKey(transaction, storage, Service.Storages.DecryptFileKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken), newParent, userAuthenticationToken, cancellationToken))
        ), cancellationToken);

        return result;
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
            parent.ThrowIfDoesNotBelongTo(storage);

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

        return Insert(transaction, new(
          (COLUMN_STORAGE_ID, storage.Id),
          (COLUMN_KEY, Service.Storages.EncryptFileKey(transaction, storage, Service.Server.KeyService.GetNewAesPair(), parent, userAuthenticationToken, cancellationToken)),
          (COLUMN_PARENT_FILE_ID, parent?.Id),
          (COLUMN_NAME, name),
          (COLUMN_TYPE, (byte)type)
        ), cancellationToken);
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, FileType Type, string Name) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long StorageId => Data.StorageId;
  public byte[] Key => Data.Key;
  public long? ParentId => Data.ParentId;
  public FileType Type => Data.Type;
  public string Name => Data.Name;

  public bool BelongsTo(StorageResource storage)
  {
    lock (this)
    {
      lock (storage)
      {
        return storage.Id == StorageId;
      }
    }
  }

  public void ThrowIfDoesNotBelongTo(StorageResource storage)
  {
    if (!BelongsTo(storage))
    {
      throw new ArgumentException("The specified file does not belong to storage.", nameof(storage));
    }
  }
}

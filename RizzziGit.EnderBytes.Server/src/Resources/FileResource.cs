using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Collections;
using Services;
using Utilities;

public enum FileType : byte { File, Folder }

[Flags]
public enum FileHandleFlags : byte
{
  Read = 1 << 0,
  Modify = 1 << 1,
  Exclusive = 1 << 2,

  ReadModify = Read | Modify
}

public sealed partial record FileResource(FileResource.ResourceManager Manager, long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, FileType Type, string Name) : Resource<FileResource.ResourceManager, FileResource>(Manager, Id, CreateTime, UpdateTime)
{
  public const string NAME = "File";
  public const int VERSION = 1;

  public new sealed partial class ResourceManager : Resource<ResourceManager, FileResource>.ResourceManager
  {
    public const string COLUMN_STORAGE_ID = "StorageId";
    public const string COLUMN_KEY = "AesKey";
    public const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    public const string COLUMN_TYPE = "Type";
    public const string COLUMN_NAME = "Name";

    public const string UNIQUE_INDEX_NAME = $"Index_{NAME}_{COLUMN_NAME}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.GetManager<StorageResource.ResourceManager>().ResourceDeleted += (transaction, storage, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id), cancellationToken);
      ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", file.Id), cancellationToken);
    }

    protected override FileResource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
      this, id, createTime, updateTime,

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

    public bool Move(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileResource? newParent, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      file.ThrowIfDoesNotBelongTo(storage);

      if (newParent != null)
      {
        newParent.ThrowIfDoesNotBelongTo(storage);

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

          cancellationToken.ThrowIfCancellationRequested();

          currentParent = GetById(transaction, (long)currentParent.ParentId, cancellationToken);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();

      if ((newParent != null) && (newParent.Type != FileType.Folder))
      {
        throw new ArgumentException("Invalid new parent.", nameof(newParent));
      }

      bool result = Update(transaction, file, new(
        (COLUMN_PARENT_FILE_ID, newParent?.Id),
        (COLUMN_KEY, Service.GetManager<StorageResource.ResourceManager>().EncryptFileKey(transaction, storage, Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken).Key, newParent, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken))
      ), cancellationToken);

      return result;
    }

    public FileResource CreateFile(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, string name, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      return Create(transaction, storage, parent, FileType.File, name, userAuthenticationToken, cancellationToken);
    }

    public FileResource CreateFolder(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, string name, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      return Create(transaction, storage, parent, FileType.Folder, name, userAuthenticationToken, cancellationToken);
    }

    private FileResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, FileType type, string name, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      parent?.ThrowIfDoesNotBelongTo(storage);

      if (!Enum.IsDefined(type))
      {
        throw new ArgumentException("Invalid file type.", nameof(type));
      }
      else if ((parent != null) && (parent.Type != FileType.Folder))
      {
        throw new ArgumentException("Parent is not a folder.", nameof(parent));
      }

      KeyService.AesPair fileKey = Service.Server.KeyService.GetNewAesPair();

      FileResource file = InsertAndGet(transaction, new(
        (COLUMN_STORAGE_ID, storage.Id),
        (COLUMN_KEY, Service.GetManager<StorageResource.ResourceManager>().EncryptFileKey(transaction, storage, fileKey, parent, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken)),
        (COLUMN_PARENT_FILE_ID, parent?.Id),
        (COLUMN_NAME, name),
        (COLUMN_TYPE, (byte)type)
      ), cancellationToken);

      return file;
    }

    public bool Delete(ResourceService.Transaction transaction, StorageResource storage, FileResource file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      file.ThrowIfDoesNotBelongTo(storage);

      Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken);

      return base.Delete(transaction, file, cancellationToken);
    }

    public override bool Delete(ResourceService.Transaction transaction, FileResource file, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException("Please specify user token.");
    }

    public FileResource? ResolvePath(ResourceService.Transaction transaction, StorageResource storage, string[] path, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      FileResource? file = null;
      for (int index = 0; index < path.Length; index++)
      {
        file = SelectOne(transaction, new WhereClause.Nested("and",
          new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id),
          new WhereClause.Raw($"lower({COLUMN_NAME}) = {{0}}", path[index].ToLower())
        ), null, null, cancellationToken);
      }

      _ = Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);
      return file;
    }

    public IEnumerable<FileResource> ScanFolder(ResourceService.Transaction transaction, StorageResource storage, FileResource? folder, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      if (folder != null)
      {
        Service.GetManager<StorageResource.ResourceManager>().DecryptKey(transaction, storage, folder, userAuthenticationToken, FileAccessType.Read, cancellationToken);
      }

      foreach (FileResource file in Select(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id),
        new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", folder?.Id)
      ), null, null, cancellationToken))
      {
        yield return file;
      }

      yield break;
    }

    public bool IsInsideOf(ResourceService.Transaction transaction, StorageResource storage, FileResource haystack, FileResource needle, CancellationToken cancellationToken = default)
    {
      haystack.ThrowIfDoesNotBelongTo(storage);
      needle.ThrowIfDoesNotBelongTo(storage);

      FileResource? current = needle;
      while (true)
      {
        if (current.ParentId == haystack.ParentId || current.ParentId == haystack.Id)
        {
          return true;
        }

        if (current.ParentId == null || !TryGetById(transaction, (long)current.ParentId, out current, cancellationToken))
        {
          break;
        }
      }

      return false;
    }
  }

  [JsonIgnore]
  public byte[] Key = Key;

  public bool BelongsTo(StorageResource storage)
  {
    return storage.Id == StorageId;
  }

  public void ThrowIfDoesNotBelongTo(StorageResource storage)
  {
    if (!BelongsTo(storage))
    {
      throw new ArgumentException("The specified file does not belong to storage.", nameof(storage));
    }
  }
}

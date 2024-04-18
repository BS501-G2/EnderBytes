using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

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

public sealed partial class FileManager : ResourceManager<FileManager, FileManager.Resource, FileManager.Exception>
{
  public abstract class Exception(string? message = null) : ResourceService.Exception(message);
  public sealed class FileDontBelongToStorageException(StorageManager.Resource storage, Resource file) : Exception($"File #{file.Id} does not belong to storage #{storage.Id}.")
  {
    public readonly StorageManager.Resource Storage = storage;
    public readonly Resource File = file;
  }
  public sealed class FileTreeException(Resource file, Resource destination) : Exception($"Moving file #{file.Id} to #{destination.Id} may close the loop.")
  {
    public readonly Resource File = file;
    public readonly Resource Destination = destination;
  }
  public sealed class NotAFolderException(Resource file) : Exception($"Resource #{file.Id} a folder.")
  {
    public readonly Resource File = file;
  }
  public sealed class NotAFileException(Resource file) : Exception($"Resource #{file.Id} a file.")
  {
    public readonly Resource File = file;
  }
  public sealed class InvalidFileTypeException(FileType type) : Exception($"Invalid file type: {(byte)type}.")
  {
    public readonly FileType Type = type;
  }
  public sealed class NotSupportedException(string? message) : Exception(message ?? "This operation is not supported.");

  public sealed new partial record Resource(long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, FileType Type, string Name) : ResourceManager<FileManager, Resource, Exception>.Resource(Id, CreateTime, UpdateTime)
  {
    [JsonIgnore]
    public byte[] Key = Key;

    public bool BelongsTo(StorageManager.Resource storage)
    {
      return storage.Id == StorageId;
    }

    public void ThrowIfDoesNotBelongTo(StorageManager.Resource storage)
    {
      if (!BelongsTo(storage))
      {
        throw new FileDontBelongToStorageException(storage, this);
      }
    }
  }

  public const string NAME = "File";
  public const int VERSION = 1;

  public const string COLUMN_STORAGE_ID = "StorageId";
  public const string COLUMN_KEY = "AesKey";
  public const string COLUMN_PARENT_FILE_ID = "ParentFileId";
  public const string COLUMN_TYPE = "Type";
  public const string COLUMN_NAME = "Name";

  public const string UNIQUE_INDEX_NAME = $"Index_{NAME}_{COLUMN_NAME}";

  public FileManager(ResourceService service) : base(service, NAME, VERSION)
  {
    Service.GetManager<StorageManager>().ResourceDeleted += (transaction, storage, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id), cancellationToken);
    ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", file.Id), cancellationToken);
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
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

  public bool Move(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource file, Resource? newParent, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    file.ThrowIfDoesNotBelongTo(storage);

    if (newParent != null)
    {
      newParent.ThrowIfDoesNotBelongTo(storage);

      if (
        IsEqualToOrInsideOf(transaction, storage, newParent, file, cancellationToken) ||
        IsEqualToOrInsideOf(transaction, storage, file, newParent, cancellationToken)
      )
      {
        throw new FileTreeException(file, newParent);
      }
    }

    cancellationToken.ThrowIfCancellationRequested();

    if ((newParent != null) && (newParent.Type != FileType.Folder))
    {
      throw new NotAFolderException(file);
    }

    bool result = Update(transaction, file, new(
      (COLUMN_PARENT_FILE_ID, newParent?.Id),
      (COLUMN_KEY, Service.GetManager<StorageManager>().EncryptFileKey(transaction, storage, Service.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken).Key, newParent, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken))
    ), cancellationToken);

    return result;
  }

  public Resource CreateFile(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource? parent, string name, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    return Create(transaction, storage, parent, FileType.File, name, userAuthenticationToken, cancellationToken);
  }

  public Resource CreateFolder(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource? parent, string name, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    return Create(transaction, storage, parent, FileType.Folder, name, userAuthenticationToken, cancellationToken);
  }

  private Resource Create(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource? parent, FileType type, string name, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    parent?.ThrowIfDoesNotBelongTo(storage);

    if (!Enum.IsDefined(type))
    {
      throw new InvalidFileTypeException(type);
    }
    else if ((parent != null) && (parent.Type != FileType.Folder))
    {
      throw new NotAFolderException(parent);
    }

    KeyService.AesPair fileKey = Service.Server.KeyService.GetNewAesPair();

    Resource file = InsertAndGet(transaction, new(
      (COLUMN_STORAGE_ID, storage.Id),
      (COLUMN_KEY, Service.GetManager<StorageManager>().EncryptFileKey(transaction, storage, fileKey, parent, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken)),
      (COLUMN_PARENT_FILE_ID, parent?.Id),
      (COLUMN_NAME, name),
      (COLUMN_TYPE, (byte)type)
    ), cancellationToken);

    return file;
  }

  public bool Delete(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource file, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    file.ThrowIfDoesNotBelongTo(storage);

    Service.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken);

    return base.Delete(transaction, file, cancellationToken);
  }

  public override bool Delete(ResourceService.Transaction transaction, Resource file, CancellationToken cancellationToken = default)
  {
    throw new NotSupportedException("Deleting a file without providing user token is not supported.");
  }

  public Resource? ResolvePath(ResourceService.Transaction transaction, StorageManager.Resource storage, string[] path, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    Resource? file = null;
    for (int index = 0; index < path.Length; index++)
    {
      file = SelectOne(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id),
        new WhereClause.Raw($"lower({COLUMN_NAME}) = {{0}}", path[index].ToLower())
      ), null, null, cancellationToken);
    }

    _ = Service.GetManager<StorageManager>().DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);
    return file;
  }

  public IEnumerable<Resource> ScanFolder(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource? folder, UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    if (folder != null)
    {
      _ = Service.GetManager<StorageManager>().DecryptKey(transaction, storage, folder, userAuthenticationToken, FileAccessType.Read, cancellationToken);
    }

    foreach (Resource file in Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id),
      new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", folder?.Id)
    ), null, null, cancellationToken))
    {
      yield return file;
    }

    yield break;
  }

  public bool IsEqualToOrInsideOf(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource haystack, Resource needle, CancellationToken cancellationToken = default)
  {
    return IsInsideOf(transaction, storage, haystack, needle, cancellationToken) || needle.Id == haystack.Id;
  }

  public bool IsInsideOf(ResourceService.Transaction transaction, StorageManager.Resource storage, Resource haystack, Resource needle, CancellationToken cancellationToken = default)
  {
    haystack.ThrowIfDoesNotBelongTo(storage);
    needle.ThrowIfDoesNotBelongTo(storage);

    Resource? current = needle;
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
using System.Data.Common;
using System.Text.Json.Serialization;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed record DecryptedKeyInfo(KeyService.AesPair Key, FileAccessManager.Resource? FileAccess);

public sealed class StorageManager : ResourceManager<StorageManager, StorageManager.Resource, StorageManager.Exception>
{
  public abstract class Exception(string? message = null) : ResourceService.Exception(message);

  public sealed class AccessDenied()
  {

  }

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long OwnerUserId,
    byte[] Key,

    long? RootFolderId,
    long? TrashFolderId,
    long? InternalFolderId
  ) : ResourceManager<StorageManager, Resource, Exception>.Resource(Id, CreateTime, UpdateTime)
  {
    [JsonIgnore]
    public byte[] Key = Key;

    [JsonIgnore]
    public long? RootFolderId = RootFolderId;
    [JsonIgnore]
    public long? TrashFolderId = TrashFolderId;
    [JsonIgnore]
    public long? InternalFolderId = InternalFolderId;
  }

  public const string NAME = "Storage";
  public const int VERSION = 1;

  public const string COLUMN_OWNER_USER_ID = "OwnerUserId";
  public const string COLUMN_KEY = "AesKey";

  public const string COLUMN_ROOT_FOLDER_ID = "RootFolderId";
  public const string COLUMN_TRASH_FOLDER_ID = "TrashFolderId";
  public const string COLUMN_INTERNAL_FOLDER_ID = "InternalFolderId";

  public StorageManager(ResourceService service) : base(service, NAME, VERSION)
  {
    service.GetManager<UserManager>().ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id), cancellationToken);
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_OWNER_USER_ID)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),

    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_ROOT_FOLDER_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TRASH_FOLDER_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_INTERNAL_FOLDER_ID))
  );

  protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
  {
    if (oldVersion < 1)
    {
      SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_OWNER_USER_ID} bigint not null;");
      SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");

      SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ROOT_FOLDER_ID} bigint null;");
      SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TRASH_FOLDER_ID} bigint null;");
      SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INTERNAL_FOLDER_ID} bigint null;");
    }
  }

  public Resource Create(ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    return InsertAndGet(transaction, new(
      (COLUMN_OWNER_USER_ID, userAuthenticationToken.UserId),
      (COLUMN_KEY, userAuthenticationToken.Encrypt(Service.Server.KeyService.GetNewAesPair().Serialize()))
    ), cancellationToken);
  }

  public bool Update(ResourceService.Transaction transaction, Resource storage, long? rootFolderId, long? trashFolderId, long? internalFolderId, CancellationToken cancellationToken = default)
  {
    return Update(transaction, storage, new(
      (COLUMN_ROOT_FOLDER_ID, rootFolderId),
      (COLUMN_TRASH_FOLDER_ID, trashFolderId),
      (COLUMN_INTERNAL_FOLDER_ID, internalFolderId)
    ), cancellationToken);
  }

  public byte[] EncryptFileKey(ResourceService.Transaction transaction, Resource storage, KeyService.AesPair key, FileManager.Resource? parent, UserAuthenticationToken? userAuthenticationToken, FileAccessType fileAccessType, CancellationToken cancellationToken = default)
  {

    if (parent == null)
    {
      if (storage.OwnerUserId != userAuthenticationToken?.UserId)
      {
        throw new ArgumentException("Other users cannot encrypt using the storage key.", nameof(userAuthenticationToken));
      }

      KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

      return encryptFileKey(storageKey, null);
    }

    return encryptFileKey(null, parent);

    byte[] encryptFileKey(KeyService.AesPair? storageKey, FileManager.Resource? parent) => storageKey == null && parent != null
      ? DecryptKey(transaction, storage, parent, userAuthenticationToken, fileAccessType, cancellationToken).Key.Encrypt(key.Serialize())
      : storageKey!.Encrypt(key.Serialize());
  }

  public DecryptedKeyInfo DecryptKey(ResourceService.Transaction transaction, Resource storage, FileManager.Resource? file, UserAuthenticationToken? userAuthenticationToken, FileAccessType? fileAccessType = null, CancellationToken cancellationToken = default)
  {
    if (file == null)
    {
      if (userAuthenticationToken?.UserId != storage.OwnerUserId)
      {
        throw new ArgumentException("The owner's authentication token is required to decrypt the storage key.", nameof(userAuthenticationToken));
      }

      return new(KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key)), null);
    }

    FileAccessManager.Resource? fileAccessUsed = null;

    if (storage.OwnerUserId != userAuthenticationToken?.UserId)
    {
      return new(decryptFileKey2(file), fileAccessUsed);

      KeyService.AesPair decryptFileKey2(FileManager.Resource file)
      {
        foreach (FileAccessManager.Resource fileAccess in Service.GetManager<FileAccessManager>().List(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken))
        {
          if (fileAccess.Type > fileAccessType)
          {
            continue;
          }

          switch (fileAccess.TargetEntityType)
          {
            case FileAccessTargetEntityType.User:
              if (userAuthenticationToken?.UserId != fileAccess.TargetEntityId)
              {
                break;
              }

              fileAccessUsed = fileAccess;
              return KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(fileAccess.AesKey));

            case FileAccessTargetEntityType.None:
              fileAccessUsed = fileAccess;
              return KeyService.AesPair.Deserialize(fileAccess.AesKey);
          }
        }

        if (file.ParentId == null)
        {
          throw new ArgumentException($"No {fileAccessType} access to the file.", nameof(userAuthenticationToken));
        }

        return KeyService.AesPair.Deserialize(decryptFileKey2(Service.GetManager<FileManager>().GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key));
      }
    }
    else
    {
      KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

      return new(decryptFileKey2(file), fileAccessUsed);

      KeyService.AesPair decryptFileKey2(FileManager.Resource file) => file.ParentId != null
        ? KeyService.AesPair.Deserialize(decryptFileKey2(Service.GetManager<FileManager>().GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key))
        : KeyService.AesPair.Deserialize(storageKey.Decrypt(file.Key));
    }
  }

  public Resource GetByOwnerUser(ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    Resource? storage = SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", userAuthenticationToken.UserId));

    if (storage == null)
    {
      storage = Create(transaction, userAuthenticationToken);
    }

    return storage;
  }

  public FileManager.Resource GetRootFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    if (storage.RootFolderId != null && transaction.GetManager<FileManager>().TryGetById(transaction, (long)storage.RootFolderId, out FileManager.Resource? file, cancellationToken))
    {
      return file;
    }

    FileManager.Resource newFile = transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_ROOT", userAuthenticationToken, cancellationToken);

    Update(transaction, storage, new(
      (COLUMN_ROOT_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }

  public FileManager.Resource GetTrashFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    if (storage.TrashFolderId != null && transaction.GetManager<FileManager>().TryGetById(transaction, (long)storage.TrashFolderId, out FileManager.Resource? file, cancellationToken))
    {
      return file;
    }

    FileManager.Resource newFile = transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_TRASH", userAuthenticationToken, cancellationToken);

    Update(transaction, storage, new(
      (COLUMN_TRASH_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }

  public FileManager.Resource GetInternalFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    if (storage.InternalFolderId != null && transaction.GetManager<FileManager>().TryGetById(transaction, (long)storage.InternalFolderId, out FileManager.Resource? file, cancellationToken))
    {
      return file;
    }

    FileManager.Resource newFile = transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_INTERNAL", userAuthenticationToken, cancellationToken);

    Update(transaction, storage, new(
      (COLUMN_INTERNAL_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }
}

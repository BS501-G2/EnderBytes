using System.Data.Common;
using System.Text.Json.Serialization;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed record StorageResource(StorageResource.ResourceManager Manager,
  long Id,
  long CreateTime,
  long UpdateTime,

  long OwnerUserId,
  byte[] Key,

  long? RootFolderId,
  long? TrashFolderId,
  long? InternalFolderId
) : Resource<StorageResource.ResourceManager, StorageResource>(Manager, Id, CreateTime, UpdateTime)
{
  public sealed record DecryptedKeyInfo(KeyService.AesPair Key, FileAccessResource? FileAccess);

  public const string NAME = "Storage";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, StorageResource>.ResourceManager
  {
    public const string COLUMN_OWNER_USER_ID = "OwnerUserId";
    public const string COLUMN_KEY = "AesKey";

    public const string COLUMN_ROOT_FOLDER_ID = "RootFolderId";
    public const string COLUMN_TRASH_FOLDER_ID = "TrashFolderId";
    public const string COLUMN_INTERNAL_FOLDER_ID = "InternalFolderId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.GetManager<UserResource.ResourceManager>().ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id), cancellationToken);
    }

    protected override StorageResource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
      this, id, createTime, updateTime,

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

    public StorageResource Create(ResourceService.Transaction transaction, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      return InsertAndGet(transaction, new(
        (COLUMN_OWNER_USER_ID, userAuthenticationToken.UserId),
        (COLUMN_KEY, userAuthenticationToken.Encrypt(Service.Server.KeyService.GetNewAesPair().Serialize()))
      ), cancellationToken);
    }

    public bool Update(ResourceService.Transaction transaction, StorageResource storage, long? rootFolderId, long? trashFolderId, long? internalFolderId, CancellationToken cancellationToken = default)
    {
      return Update(transaction, storage, new(
        (COLUMN_ROOT_FOLDER_ID, rootFolderId),
        (COLUMN_TRASH_FOLDER_ID, trashFolderId),
        (COLUMN_INTERNAL_FOLDER_ID, internalFolderId)
      ), cancellationToken);
    }

    public byte[] EncryptFileKey(ResourceService.Transaction transaction, StorageResource storage, KeyService.AesPair key, FileResource? parent, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, FileAccessType fileAccessType, CancellationToken cancellationToken = default)
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

      byte[] encryptFileKey(KeyService.AesPair? storageKey, FileResource? parent) => storageKey == null && parent != null
        ? DecryptKey(transaction, storage, parent, userAuthenticationToken, fileAccessType, cancellationToken).Key.Encrypt(key.Serialize())
        : storageKey!.Encrypt(key.Serialize());
    }

    public DecryptedKeyInfo DecryptKey(ResourceService.Transaction transaction, StorageResource storage, FileResource? file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, FileAccessType? fileAccessType = null, CancellationToken cancellationToken = default)
    {
      if (file == null)
      {
        if (userAuthenticationToken?.UserId != storage.OwnerUserId)
        {
          throw new ArgumentException("The owner's authentication token is required to decrypt the storage key.", nameof(userAuthenticationToken));
        }

        return new(KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key)), null);
      }

      FileAccessResource? fileAccessUsed = null;

      if (storage.OwnerUserId != userAuthenticationToken?.UserId)
      {
        return new(decryptFileKey2(file), fileAccessUsed);

        KeyService.AesPair decryptFileKey2(FileResource file)
        {
          foreach (FileAccessResource fileAccess in Service.GetManager<FileAccessResource.ResourceManager>().List(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken))
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

          return KeyService.AesPair.Deserialize(decryptFileKey2(Service.GetManager<FileResource.ResourceManager>().GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key));
        }
      }
      else
      {
        KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

        return new(decryptFileKey2(file), fileAccessUsed);

        KeyService.AesPair decryptFileKey2(FileResource file) => file.ParentId != null
          ? KeyService.AesPair.Deserialize(decryptFileKey2(Service.GetManager<FileResource.ResourceManager>().GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key))
          : KeyService.AesPair.Deserialize(storageKey.Decrypt(file.Key));
      }
    }

    public StorageResource GetByOwnerUser(ResourceService.Transaction transaction, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      StorageResource? storage = SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", userAuthenticationToken.UserId));

      if (storage == null)
      {
        storage = Create(transaction, userAuthenticationToken);
      }

      return storage;
    }

    public FileResource GetRootFolder(ResourceService.Transaction transaction, StorageResource storage, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      if (storage.RootFolderId != null && transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, (long)storage.RootFolderId, out FileResource? file, cancellationToken))
      {
        return file;
      }

      FileResource newFile = transaction.GetManager<FileResource.ResourceManager>().CreateFolder(transaction, storage, null, "_ROOT", userAuthenticationToken, cancellationToken);

      Update(transaction, storage, new(
        (COLUMN_ROOT_FOLDER_ID, newFile.Id)
      ), cancellationToken);

      return newFile;
    }

    public FileResource GetTrashFolder(ResourceService.Transaction transaction, StorageResource storage, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      if (storage.TrashFolderId != null && transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, (long)storage.TrashFolderId, out FileResource? file, cancellationToken))
      {
        return file;
      }

      FileResource newFile = transaction.GetManager<FileResource.ResourceManager>().CreateFolder(transaction, storage, null, "_TRASH", userAuthenticationToken, cancellationToken);

      Update(transaction, storage, new(
        (COLUMN_TRASH_FOLDER_ID, newFile.Id)
      ), cancellationToken);

      return newFile;
    }

    public FileResource GetInternalFolder(ResourceService.Transaction transaction, StorageResource storage, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      if (storage.InternalFolderId != null && transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, (long)storage.InternalFolderId, out FileResource? file, cancellationToken))
      {
        return file;
      }

      FileResource newFile = transaction.GetManager<FileResource.ResourceManager>().CreateFolder(transaction, storage, null, "_INTERNAL", userAuthenticationToken, cancellationToken);

      Update(transaction, storage, new(
        (COLUMN_INTERNAL_FOLDER_ID, newFile.Id)
      ), cancellationToken);

      return newFile;
    }
  }

  [JsonIgnore]
  public byte[] Key = Key;

  [JsonIgnore]
  public long? RootFolderId = RootFolderId;
  [JsonIgnore]
  public long? TrashFolderId = TrashFolderId;
  [JsonIgnore]
  public long? InternalFolderId = InternalFolderId;
}

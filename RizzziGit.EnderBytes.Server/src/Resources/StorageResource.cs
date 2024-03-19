using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class StorageResource(StorageResource.ResourceManager manager, StorageResource.ResourceData data) : Resource<StorageResource.ResourceManager, StorageResource.ResourceData, StorageResource>(manager, data)
{
  public sealed record DecryptedKeyInfo(KeyService.AesPair Key, FileAccessResource? FileAccess);

  public const string NAME = "Storage";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, StorageResource>.ResourceManager
  {
    public const string COLUMN_OWNER_USER_ID = "OwnerUserId";
    public const string COLUMN_KEY = "AesKey";

    public const string COLUMN_ROOT_FOLDER_ID = "RootFolderId";
    public const string COLUMN_TRASH_FOLDER_ID = "TrashFolderId";
    public const string COLUMN_INTERNAL_FOLDER_ID = "InternalFolderId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id), cancellationToken);
    }

    protected override StorageResource NewResource(ResourceData data) => new(this, data);

    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
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

    public StorageResource Create(ResourceService.Transaction transaction, UserResource user, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (user)
      {
        user.ThrowIfInvalid();

        return userAuthenticationToken.Enter(() =>
        {
          userAuthenticationToken.ThrowIfInvalid();

          return InsertAndGet(transaction, new(
            (COLUMN_OWNER_USER_ID, user.Id),
            (COLUMN_KEY, userAuthenticationToken.Encrypt(Service.Server.KeyService.GetNewAesPair().Serialize()))
          ), cancellationToken);
        });
      }
    }

    public bool Update(ResourceService.Transaction transaction, StorageResource storage, long? rootFolderId, long? trashFolderId, long? internalFolderId, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        return Update(transaction, storage, new(
          (COLUMN_ROOT_FOLDER_ID, rootFolderId),
          (COLUMN_TRASH_FOLDER_ID, trashFolderId),
          (COLUMN_INTERNAL_FOLDER_ID, internalFolderId)
        ), cancellationToken);
      }
    }

    public byte[] EncryptFileKey(ResourceService.Transaction transaction, StorageResource storage, KeyService.AesPair key, FileResource? parent, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, FileAccessResource.FileAccessType fileAccessType, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        if (parent == null)
        {
          if (storage.OwnerUserId != userAuthenticationToken?.UserId)
          {
            throw new ArgumentException("Other users cannot encrypt using the storage key.", nameof(userAuthenticationToken));
          }

          KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

          return encryptFileKey(storageKey, null);
        }

        lock (parent)
        {
          parent.ThrowIfInvalid();

          return encryptFileKey(null, parent);
        }

        byte[] encryptFileKey(KeyService.AesPair? storageKey, FileResource? parent) => storageKey == null && parent != null
          ? DecryptKey(transaction, storage, parent, userAuthenticationToken, fileAccessType, cancellationToken).Key.Encrypt(key.Serialize())
          : storageKey!.Encrypt(key.Serialize());
      }
    }

    public DecryptedKeyInfo DecryptKey(ResourceService.Transaction transaction, StorageResource storage, FileResource? file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, FileAccessResource.FileAccessType? fileAccessType = null, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        if (file == null)
        {
          if (userAuthenticationToken?.UserId != storage.OwnerUserId)
          {
            throw new ArgumentException("The owner's authentication token is required to decrypt the storage key.", nameof(userAuthenticationToken));
          }

          return new(KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key)), null);
        }

        lock (file)
        {
          file.ThrowIfInvalid();

          FileAccessResource? fileAccessUsed = null;

          if (userAuthenticationToken == null)
          {
            return new(decryptFileKey(), fileAccessUsed);
          }

          return userAuthenticationToken.Enter(() => new DecryptedKeyInfo(decryptFileKey(), fileAccessUsed));

          KeyService.AesPair decryptFileKey()
          {
            if (storage.OwnerUserId != userAuthenticationToken?.UserId)
            {
              return decryptFileKey2(file);

              KeyService.AesPair decryptFileKey2(FileResource file)
              {
                foreach (FileAccessResource fileAccess in Service.FileAccesses.List(transaction, file, cancellationToken: cancellationToken))
                {
                  if (fileAccess.Type > fileAccessType)
                  {
                    continue;
                  }

                  switch (fileAccess.TargetEntityType)
                  {
                    case FileAccessResource.FileAccessTargetEntityType.User:
                      if (userAuthenticationToken?.UserId != fileAccess.TargetEntityId)
                      {
                        break;
                      }

                      fileAccessUsed = fileAccess;
                      return KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(fileAccess.Key));

                    case FileAccessResource.FileAccessTargetEntityType.None:
                      fileAccessUsed = fileAccess;
                      return KeyService.AesPair.Deserialize(fileAccess.Key);
                  }
                }

                if (file.ParentId == null)
                {
                  throw new ArgumentException($"No {fileAccessType} access to the file.", nameof(userAuthenticationToken));
                }

                return KeyService.AesPair.Deserialize(decryptFileKey2(Service.Files.GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key));
              }
            }
            else
            {
              KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

              return decryptFileKey2(file);

              KeyService.AesPair decryptFileKey2(FileResource file) => file.ParentId != null
                ? KeyService.AesPair.Deserialize(decryptFileKey2(Service.Files.GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key))
                : KeyService.AesPair.Deserialize(storageKey.Decrypt(file.Key));
            }
          }
        }
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long OwnerUserId,
    byte[] Key,

    long? RootFolderId,
    long? TrashFolderId,
    long? InternalFolderId
  ) : Resource<ResourceManager, ResourceData, StorageResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long OwnerUserId => Data.OwnerUserId;
  public byte[] Key => Data.Key;

  public long? RootFolderId => Data.RootFolderId;
  public long? TrashFolderId => Data.TrashFolderId;
  public long? InternalFolderId => Data.InternalFolderId;
}

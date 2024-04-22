using System.Data.Common;
using System.Text.Json.Serialization;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed record DecryptedKeyInfo(KeyService.AesPair Key, FileAccessManager.Resource? FileAccess);

public sealed class StorageManager : ResourceManager<StorageManager, StorageManager.Resource, StorageManager.Exception>
{
  public abstract class Exception(string? message = null) : ResourceService.Exception(message);

  public sealed class StorageEncryptDeniedException() : Exception("Other users cannot encrypt using the storage key.");
  public sealed class StorageDecryptDeniedException() : Exception("The owner's authentication token is required to decrypt the storage key.");
  public sealed class AccessDeniedException(FileAccessType type) : Exception($"No {type} access to the file.");

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
    service.GetManager<UserManager>().RegisterDeleteHandler((transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id), cancellationToken));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64(reader.GetOrdinal(COLUMN_OWNER_USER_ID)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),

    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_ROOT_FOLDER_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TRASH_FOLDER_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_INTERNAL_FOLDER_ID))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_OWNER_USER_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");

      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ROOT_FOLDER_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TRASH_FOLDER_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_INTERNAL_FOLDER_ID} bigint null;");
    }
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    return await InsertAndGet(transaction, new(
      (COLUMN_OWNER_USER_ID, userAuthenticationToken.UserId),
      (COLUMN_KEY, userAuthenticationToken.Encrypt(Service.Server.KeyService.GetNewAesPair().Serialize()))
    ), cancellationToken);
  }

  public async Task<bool> Update(ResourceService.Transaction transaction, Resource storage, long? rootFolderId, long? trashFolderId, long? internalFolderId, CancellationToken cancellationToken = default)
  {
    return await Update(transaction, storage, new(
      (COLUMN_ROOT_FOLDER_ID, rootFolderId),
      (COLUMN_TRASH_FOLDER_ID, trashFolderId),
      (COLUMN_INTERNAL_FOLDER_ID, internalFolderId)
    ), cancellationToken);
  }

  public Task<byte[]> EncryptFileKey(ResourceService.Transaction transaction, Resource storage, KeyService.AesPair key, FileManager.Resource? parent, UserAuthenticationToken userAuthenticationToken, FileAccessType fileAccessType, CancellationToken cancellationToken = default)
  {

    if (parent == null)
    {
      if (storage.OwnerUserId != userAuthenticationToken.UserId)
      {
        throw new StorageEncryptDeniedException();
      }

      KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

      return encryptFileKey(storageKey, null);
    }

    return encryptFileKey(null, parent);

    async Task<byte[]> encryptFileKey(KeyService.AesPair? storageKey, FileManager.Resource? parent) => storageKey == null && parent != null
      ? (await DecryptKey(transaction, storage, parent, userAuthenticationToken, fileAccessType, cancellationToken)).Key.Encrypt(key.Serialize())
      : storageKey!.Encrypt(key.Serialize());
  }

  public async Task<DecryptedKeyInfo> DecryptKey(ResourceService.Transaction transaction, Resource storage, FileManager.Resource? file, UserAuthenticationToken userAuthenticationToken, FileAccessType fileAccessType, CancellationToken cancellationToken = default)
  {
    if (file == null)
    {
      if (userAuthenticationToken.UserId != storage.OwnerUserId)
      {
        throw new StorageDecryptDeniedException();
      }

      return new(KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key)), null);
    }

    FileAccessManager.Resource? fileAccessUsed = null;

    if (storage.OwnerUserId != userAuthenticationToken.UserId)
    {
      return new(await decryptFileKey2(file), fileAccessUsed);

      async Task<KeyService.AesPair> decryptFileKey2(FileManager.Resource file)
      {
        await foreach (FileAccessManager.Resource fileAccess in Service.GetManager<FileAccessManager>().List(transaction, storage, file, cancellationToken: cancellationToken))
        {
          if (fileAccess.Type > fileAccessType)
          {
            continue;
          }

          switch (fileAccess.TargetEntityType)
          {
            case FileAccessTargetEntityType.User:
              if (userAuthenticationToken.UserId != fileAccess.TargetEntityId)
              {
                break;
              }

              fileAccessUsed = fileAccess;
              return KeyService.AesPair.Deserialize(userAuthenticationToken!.Decrypt(fileAccess.AesKey));

            case FileAccessTargetEntityType.None:
              fileAccessUsed = fileAccess;
              return KeyService.AesPair.Deserialize(fileAccess.AesKey);
          }
        }

        if (file.ParentId == null)
        {
          throw new AccessDeniedException(fileAccessType!);
        }

        return KeyService.AesPair.Deserialize((await decryptFileKey2(await Service.GetManager<FileManager>().GetByRequiredId(transaction, (long)file.ParentId, cancellationToken))).Decrypt(file.Key));
      }
    }
    else
    {
      KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

      return new(await decryptFileKey2(file), fileAccessUsed);

      async Task<KeyService.AesPair> decryptFileKey2(FileManager.Resource file) => file.ParentId != null
        ? KeyService.AesPair.Deserialize((await decryptFileKey2(await Service.GetManager<FileManager>().GetByRequiredId(transaction, (long)file.ParentId, cancellationToken))).Decrypt(file.Key))
        : KeyService.AesPair.Deserialize(storageKey.Decrypt(file.Key));
    }
  }

  public async Task<Resource> GetByOwnerUser(ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    Resource? storage = await SelectOne(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", userAuthenticationToken.UserId));

    if (storage == null)
    {
      storage = await Create(transaction, userAuthenticationToken, cancellationToken);
    }

    return storage;
  }

  public async Task<FileManager.Resource> GetRootFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    FileManager.Resource? file;
    if (storage.RootFolderId != null && (file = await transaction.GetManager<FileManager>().GetById(transaction, (long)storage.RootFolderId, cancellationToken)) != null)
    {
      return file;
    }

    FileManager.Resource newFile = await transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_ROOT", userAuthenticationToken, cancellationToken);
    await Update(transaction, storage, new(
      (COLUMN_ROOT_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }

  public async Task<FileManager.Resource> GetTrashFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    FileManager.Resource? file;
    if (storage.TrashFolderId != null && (file = await transaction.GetManager<FileManager>().GetById(transaction, (long)storage.TrashFolderId, cancellationToken)) != null)
    {
      return file;
    }

    FileManager.Resource newFile = await transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_TRASH", userAuthenticationToken, cancellationToken);

    await Update(transaction, storage, new(
      (COLUMN_TRASH_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }

  public async Task<FileManager.Resource> GetInternalFolder(ResourceService.Transaction transaction, Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    FileManager.Resource? file;
    if (storage.InternalFolderId != null && (file = await transaction.GetManager<FileManager>().GetById(transaction, (long)storage.InternalFolderId, cancellationToken)) != null)
    {
      return file;
    }

    FileManager.Resource newFile = await transaction.GetManager<FileManager>().CreateFolder(transaction, storage, null, "_INTERNAL", userAuthenticationToken, cancellationToken);

    await Update(transaction, storage, new(
      (COLUMN_INTERNAL_FOLDER_ID, newFile.Id)
    ), cancellationToken);

    return newFile;
  }
}

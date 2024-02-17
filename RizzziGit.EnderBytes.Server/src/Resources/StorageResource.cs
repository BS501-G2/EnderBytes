using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class StorageResource(StorageResource.ResourceManager manager, StorageResource.ResourceData data) : Resource<StorageResource.ResourceManager, StorageResource.ResourceData, StorageResource>(manager, data)
{
  // [Flags]
  // public enum StorageFlags : byte { }

  private const string NAME = "Storage";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, StorageResource>.ResourceManager
  {
    private const string COLUMN_OWNER_USER_ID = "OwnerUserId";
    private const string COLUMN_KEY = "AesKey";

    private const string COLUMN_ROOT_FOLDER_ID = "RootFolderId";
    private const string COLUMN_TRASH_FOLDER_ID = "TrashFolderId";
    private const string COLUMN_INTERNAL_FOLDER_ID = "InternalFolderId";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, user) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id));
    }

    protected override StorageResource NewResource(ResourceService.Transaction transaction, ResourceData data, CancellationToken cancellationToken = default) => new(this, data);

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

    public StorageResource Create(ResourceService.Transaction transaction, UserResource user, UserAuthenticationResource.UserAuthenticationToken token, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        lock (user)
        {
          user.ThrowIfInvalid();

          lock (token)
          {
            token.ThrowIfInvalid();

            return Insert(transaction, new(
              (COLUMN_OWNER_USER_ID, user.Id),
              (COLUMN_KEY, token.Encrypt(Service.Server.KeyService.GetNewAesPair().Serialize()))
            ), cancellationToken);
          }
        }
      }
    }

    public bool Update(ResourceService.Transaction transaction, StorageResource storage, long? rootFolderId, long? trashFolderId, long? internalFolderId, CancellationToken cancellationToken = default)
    {
      lock (this)
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
    }

    public byte[] EncryptFileKey(ResourceService.Transaction transaction, StorageResource storage, KeyService.AesPair key, FileResource? parent, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();

          KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

          if (parent == null)
          {
            return encryptFileKey();
          }

          lock (parent)
          {
            parent.ThrowIfInvalid();

            return encryptFileKey();
          }

          byte[] encryptFileKey() => parent != null
            ? DecryptFileKey(transaction, storage, parent, userAuthenticationToken, cancellationToken).Encrypt(key.Serialize())
            : storageKey.Encrypt(key.Serialize());
        }
      }
    }

    public KeyService.AesPair DecryptFileKey(ResourceService.Transaction transaction, StorageResource storage, FileResource file, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();

          lock (file)
          {
            file.ThrowIfInvalid();

            lock (userAuthenticationToken)
            {
              userAuthenticationToken.ThrowIfInvalid();

              if (storage.OwnerUserId != userAuthenticationToken.UserId)
              {
                throw new ArgumentException("Token does not belong to storage owner.", nameof(userAuthenticationToken));
              }

              KeyService.AesPair storageKey = KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(storage.Key));

              return decryptFileKey(file);

              KeyService.AesPair decryptFileKey(FileResource file)
              {
                if (file.ParentId != null)
                {
                  return KeyService.AesPair.Deserialize(decryptFileKey(Service.Files.GetById(transaction, (long)file.ParentId, cancellationToken)).Decrypt(file.Key));
                }

                return KeyService.AesPair.Deserialize(storageKey.Decrypt(file.Key));
              }
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

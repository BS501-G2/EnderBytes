using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using BlobStorage;
using Extensions;

public sealed class BlobKeyResource(BlobKeyResource.ResourceManager manager, BlobKeyResource.ResourceData data) : Resource<BlobKeyResource.ResourceManager, BlobKeyResource.ResourceData, BlobKeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobKeyResource>.ResourceManager
  {
    private const string NAME = "BlobKey";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_USER_KEY_ID = "UserKeyId";
    private const string KEY_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    public ResourceManager(BlobStorageResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Files.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      });

      main.Server.Resources.UserKeys.ResourceDeleted += (transaction, resource) => main.MainDatabase.RunTransaction((transaction) => DbDelete(transaction, new()
      {
        { KEY_USER_KEY_ID, ("=", resource.Id, null) }
      }), CancellationToken.None).Wait();
    }

    protected override BlobKeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_USER_KEY_ID],
      (byte[])reader[KEY_ENCRYPTED_PRIVATE_KEY],
      (byte[])reader[KEY_PUBLIC_KEY]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_KEY_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ENCRYPTED_PRIVATE_KEY} blob  not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_KEY} blob  not null;");
      }
    }

    public BlobKeyResource Create(DatabaseTransaction transaction, BlobFileResource file, UserKeyResource userKey)
    {
      foreach (BlobKeyResource _ in DbStream(transaction, new()
      {
        { KEY_FILE_ID, ("=", file.Id, null) }
      }, (1, null)))
      {
        throw new InvalidOperationException("Key already exists for the specified file resource.");
      }

      var (privateKey, publicKey) = Main.Server.KeyGenerator.GetNew();
      return DbInsert(transaction, new()
      {
        { KEY_FILE_ID, file.Id },
        { KEY_USER_KEY_ID, userKey.Id },
        { KEY_ENCRYPTED_PRIVATE_KEY, userKey.Encrypt(privateKey) },
        { KEY_PUBLIC_KEY, publicKey }
      });
    }

    public BlobKeyResource Create(
      DatabaseTransaction transaction,
      BlobFileResource file,
      (UserKeyResource userKey, byte[] hashCache) from,
      UserKeyResource toUserKey
    )
    {
      foreach (BlobKeyResource blobKey in DbStream(transaction, new()
      {
        { KEY_FILE_ID, ("=", file.Id, null) },
        { KEY_USER_KEY_ID, ("=", from.userKey.Id, null) }
      }, (1, null)))
      {
        return DbInsert(transaction, new()
        {
          { KEY_FILE_ID, file.Id },
          { KEY_USER_KEY_ID, toUserKey.Id },
          { KEY_ENCRYPTED_PRIVATE_KEY, toUserKey.Encrypt(from.userKey.Decrypt(blobKey.EncryptedPrivateKey, from.hashCache)) },
          { KEY_PUBLIC_KEY, from.userKey.PublicKey }
        });
      }

      throw new ArgumentException("User key is not valid.", nameof(from));
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long UserKeyId,
    byte[] EncryptedPrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, BlobKeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long UserKeyId => Data.UserKeyId;
  public byte[] EncryptedPrivateKey => Data.EncryptedPrivateKey;
  public byte[] PublicKey => Data.PublicKey;
}

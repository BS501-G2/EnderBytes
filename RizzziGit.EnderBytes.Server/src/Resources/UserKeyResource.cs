using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Keys;
using Extensions;

public sealed class UserKeyResource(UserKeyResource.ResourceManager manager, UserKeyResource.ResourceData data) : Resource<UserKeyResource.ResourceManager, UserKeyResource.ResourceData, UserKeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserKeyResource>.ResourceManager
  {
    private const string NAME = "UserKey";
    private const int VERSION = 1;

    private const string KEY_USER_ID = "UserId";
    private const string KEY_USER_AUTHENTICATION_ID = "UserAuthenticationId";
    private const string KEY_PRIVATE_IV = "PrivateIv";
    private const string KEY_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      RNG = RandomNumberGenerator.Create();
    }

    private readonly RandomNumberGenerator RNG;

    protected override UserKeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_USER_ID],
      (long)reader[KEY_USER_AUTHENTICATION_ID],
      (byte[])reader[KEY_PRIVATE_IV],
      (byte[])reader[KEY_ENCRYPTED_PRIVATE_KEY],
      (byte[])reader[KEY_PUBLIC_KEY]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_AUTHENTICATION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_IV} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ENCRYPTED_PRIVATE_KEY} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_KEY} blob not null;");
      }
    }

    public UserKeyResource Create(DatabaseTransaction transaction, UserResource user, UserAuthenticationResource userAuthentication, byte[] privateKey, byte[] publicKey, byte[] hashcache)
    {
      byte[] iv = RNG.GetBytes(16);

      return DbInsert(transaction, new()
      {
        { KEY_USER_ID, user.Id },
        { KEY_USER_AUTHENTICATION_ID, userAuthentication.Id },
        { KEY_PRIVATE_IV, iv },
        { KEY_ENCRYPTED_PRIVATE_KEY, Aes.Create().CreateEncryptor(hashcache, iv).TransformFinalBlock(privateKey) },
        { KEY_PUBLIC_KEY, publicKey }
      });
    }

    public UserKeyResource Create(
      DatabaseTransaction transaction,
      UserResource user,
      (UserAuthenticationResource userAuthentication, byte[] hashCache) from,
      (UserAuthenticationResource userAuthentication, byte[] hashCache) to
    )
    {
      foreach (UserKeyResource userKey in DbStream(transaction, new()
      {
        { KEY_USER_ID, ("=", user.Id, null) },
        { KEY_USER_AUTHENTICATION_ID, ("=", from.userAuthentication.Id, null) }
      }, (1, null)))
      {
        byte[] iv = RNG.GetBytes(16);
        byte[] privateKey = Aes.Create().CreateDecryptor(from.hashCache, userKey.PrivateIv).TransformFinalBlock(userKey.EncryptedPrivateKey);

        return DbInsert(transaction, new()
        {
          { KEY_USER_ID, user.Id },
          { KEY_USER_AUTHENTICATION_ID, to.userAuthentication.Id },
          { KEY_PRIVATE_IV, iv },
          { KEY_ENCRYPTED_PRIVATE_KEY, Aes.Create().CreateEncryptor(to.hashCache, iv).TransformFinalBlock(privateKey) },
          { KEY_PUBLIC_KEY, userKey.PublicKey }
        });
      }

      throw new ArgumentException("Source user authentication is not valid.", nameof(from));
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserId,
    long UserAuthenticationId,
    byte[] PrivateIv,
    byte[] EncryptedPrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, UserKeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long UserId => Data.UserId;
  public long UserAuthenticationId => Data.UserAuthenticationId;
  public byte[] PrivateIv => Data.PrivateIv;
  public byte[] EncryptedPrivateKey => Data.EncryptedPrivateKey;
  public byte[] PublicKey => Data.PublicKey;

  public byte[] Decrypt(byte[] bytes, byte[] hashCache)
  {
    using RSACryptoServiceProvider provider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KeyGenerator.KEY_SIZE
    };

    try
    {
      provider.ImportCspBlob(Aes.Create().CreateDecryptor(hashCache, PrivateIv).TransformFinalBlock(EncryptedPrivateKey));
      return provider.Encrypt(bytes, true);
    }
    finally
    {
      provider.Clear();
    }
  }

  public byte[] Encrypt(byte[] bytes, byte[]? hashCache = null)
  {
    using RSACryptoServiceProvider provider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KeyGenerator.KEY_SIZE
    };

    try
    {
      if (hashCache == null)
      {
        provider.ImportCspBlob(PublicKey);
      }
      else
      {
        provider.ImportCspBlob(Aes.Create().CreateDecryptor(hashCache, PrivateIv).TransformFinalBlock(EncryptedPrivateKey));
      }

      return provider.Encrypt(bytes, true);
    }
    finally
    {
      provider.Clear();
    }

  }
}

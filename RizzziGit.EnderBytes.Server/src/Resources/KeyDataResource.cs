using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using System.Collections;
using System.Security.Cryptography;
using Database;
using RizzziGit.EnderBytes.Extensions;

public sealed class KeyDataResource(KeyDataResource.ResourceManager manager, KeyDataResource.ResourceData data) : Resource<KeyDataResource.ResourceManager, KeyDataResource.ResourceData, KeyDataResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyDataResource>.ResourceManager
  {
    private const string NAME = "KeyData";
    private const int VERSION = 1;

    private const string KEY_KEY_ID = "KeyId";
    private const string KEY_AUTHENTICATION_USER_ID = "AuthenticationUserId";
    private const string KEY_PRIVATE_IV = "PrivateIv";
    private const string KEY_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      RNG = RandomNumberGenerator.Create();
    }

    private readonly RandomNumberGenerator RNG;

    protected override KeyDataResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_KEY_ID],
      (long)reader[KEY_AUTHENTICATION_USER_ID],
      (byte[])reader[KEY_PRIVATE_IV],
      (byte[])reader[KEY_ENCRYPTED_PRIVATE_KEY],
      (byte[])reader[KEY_PUBLIC_KEY]
    );

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_KEY_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_AUTHENTICATION_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_IV} blob not null");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ENCRYPTED_PRIVATE_KEY} blob not null");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_KEY} blob not null");
      }
    }

    public IEnumerable<KeyDataResource> List(DatabaseTransaction transaction, KeyResource key, (int count, int? offset)? limit = null, List<(string column, string orderBy)>? order = null) => DbStream(transaction, new()
    {
      { KEY_KEY_ID, ("=", key.Id, null) }
    }, limit, order);

    public KeyDataResource Create(DatabaseTransaction transaction, KeyResource key, UserAuthenticationResource userAuthentication, byte[] hashCache)
    {
      {
        using var reader = DbSelect(transaction, new()
        {
          { KEY_KEY_ID, ("=", key.Id, null) }
        }, [], (1, null), null);

        while (reader.Read())
        {
          throw new InvalidOperationException("Data already exists.");
        }
      }

      byte[] privateIv = new byte[16];
      using var rsa = new RSACryptoServiceProvider()
      {
        PersistKeyInCsp = false,
        KeySize = 1024
      };
      byte[] privatePayload = rsa.ExportRSAPrivateKey();
      byte[] publicKey = rsa.ExportRSAPublicKey();

      byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hashCache, privateIv).TransformFinalBlock(privatePayload, 0, privatePayload.Length);

      return DbInsert(transaction, new()
      {
        { KEY_KEY_ID, key.Id },
        { KEY_AUTHENTICATION_USER_ID, userAuthentication.Id },
        { KEY_PRIVATE_IV, privateIv },
        { KEY_ENCRYPTED_PRIVATE_KEY, encryptedPrivateKey },
        { KEY_PUBLIC_KEY, publicKey }
      });
    }

    public KeyDataResource Create(
      DatabaseTransaction transaction,
      KeyResource key,
      UserAuthenticationResource userAuthentication,
      byte[] hashCache,
      UserAuthenticationResource copyFromUserAuthentication,
      byte[] fromHashCache
    )
    {
      using var reader = DbSelect(transaction, new()
      {
        { KEY_KEY_ID, ("=", key.Id, null) },
        { KEY_AUTHENTICATION_USER_ID, ("=", copyFromUserAuthentication.Id, null) },
      }, [], (1, null), null);

      while (reader.Read())
      {
        KeyDataResource copyFrom = Memory.ResolveFromData(CreateData(reader));

        byte[] privateKey = Aes.Create().CreateDecryptor(fromHashCache, copyFrom.PrivateIv).TransformFinalBlock(copyFrom.EncryptedPrivateKey, 0, copyFrom.EncryptedPrivateKey.Length);
        byte[] privateIv = RNG.GetBytes(16);

        return DbInsert(transaction, new()
        {
          { KEY_KEY_ID, key.Id },
          { KEY_AUTHENTICATION_USER_ID, userAuthentication.Id },
          { KEY_PRIVATE_IV, privateIv },
          { KEY_ENCRYPTED_PRIVATE_KEY, Aes.Create().CreateEncryptor(hashCache, privateIv).TransformFinalBlock(privateKey, 0, privateKey.Length) },
          { KEY_PUBLIC_KEY, copyFrom.PublicKey }
        });
      }

      throw new InvalidOperationException("Cannot copy from specified user authentication.");
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long KeyId,
    long UserAuthenticationId,
    byte[] PrivateIv,
    byte[] EncryptedPrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, KeyDataResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long KeyId => Data.KeyId;
  public long UserAuthenticationId => Data.UserAuthenticationId;
  public byte[] PrivateIv => Data.PrivateIv;
  public byte[] EncryptedPrivateKey => Data.EncryptedPrivateKey;
  public byte[] PublicKey => Data.PublicKey;

  public byte[] Encrypt(byte[] bytes)
  {
    using var rsa = new RSACryptoServiceProvider()
    {
      PersistKeyInCsp = false,
      KeySize = 1024
    };
    rsa.ImportRSAPublicKey(PublicKey, out _);
    return rsa.Encrypt(bytes, false);
  }

  public byte[] Decrypt(byte[] bytes, byte[] hashCache)
  {
    using var rsa = new RSACryptoServiceProvider()
    {
      PersistKeyInCsp = false,
      KeySize = 1024
    };

    rsa.ImportRSAPrivateKey(Aes.Create().CreateDecryptor(hashCache, PrivateIv).TransformFinalBlock(EncryptedPrivateKey, 0, EncryptedPrivateKey.Length), out _);
    return rsa.Decrypt(bytes, false);
  }
}

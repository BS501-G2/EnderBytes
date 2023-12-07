using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Keys;
using Extensions;

public sealed class UserKeyResource(UserKeyResource.ResourceManager manager, UserKeyResource.ResourceData data) : Resource<UserKeyResource.ResourceManager, UserKeyResource.ResourceData, UserKeyResource>(manager, data)
{
  public new sealed class ResourceManager(Resources.ResourceManager main, Database database) : Resource<ResourceManager, ResourceData, UserKeyResource>.ResourceManager(main, database, NAME, VERSION)
  {
    private const string NAME = "UserKey";
    private const int VERSION = 1;

    private const string KEY_SHARED_ID = "SharedId";
    private const string KEY_USER_ID = "UserId";
    private const string KEY_USER_AUTHENTICATION_ID = "UserAuthenticationId";
    private const string KEY_PRIVATE_IV = "PrivateIv";
    private const string KEY_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    private readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();

    protected override UserKeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_SHARED_ID],
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
      long sharedId;
      do
      {
        sharedId = Random.Shared.NextInt64();
      }
      while (DbOnce(transaction, new() { { KEY_SHARED_ID, ("=", sharedId) } }) != null);

      return DbInsert(transaction, new()
      {
        { KEY_SHARED_ID, sharedId },
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
        { KEY_USER_ID, ("=", user.Id) },
        { KEY_USER_AUTHENTICATION_ID, ("=", from.userAuthentication.Id) }
      }, new(1, null)))
      {
        byte[] iv = RNG.GetBytes(16);
        byte[] privateKey = Aes.Create().CreateDecryptor(from.hashCache, userKey.PrivateIv).TransformFinalBlock(userKey.EncryptedPrivateKey);

        return DbInsert(transaction, new()
        {
          { KEY_SHARED_ID, userKey.SharedId },
          { KEY_USER_ID, user.Id },
          { KEY_USER_AUTHENTICATION_ID, to.userAuthentication.Id },
          { KEY_PRIVATE_IV, iv },
          { KEY_ENCRYPTED_PRIVATE_KEY, Aes.Create().CreateEncryptor(to.hashCache, iv).TransformFinalBlock(privateKey) },
          { KEY_PUBLIC_KEY, userKey.PublicKey }
        });
      }

      throw new ArgumentException("Source user authentication is not valid.", nameof(from));
    }

    public UserKeyResource? GetByUserAuthentication(DatabaseTransaction transaction, UserResource user, UserAuthenticationResource userAuthentication) => DbOnce(transaction, new()
    {
      { KEY_USER_ID, ("=", user.Id) },
      { KEY_USER_AUTHENTICATION_ID, ("=", userAuthentication.Id) },
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SharedId,
    long UserId,
    long UserAuthenticationId,
    byte[] PrivateIv,
    byte[] EncryptedPrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, UserKeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public sealed class Transformer(UserKeyResource userKey, RSACryptoServiceProvider serviceProvider) : IDisposable
  {
    public readonly UserKeyResource UserKey = userKey;

    private readonly RSACryptoServiceProvider Provider = serviceProvider;

    public byte[] Encrypt(byte[] bytes)
    {
      UserKey.ThrowIfInvalid();
      return Provider.Encrypt(bytes, true);
    }

    public byte[] Decrypt(byte[] bytes)
    {
      UserKey.ThrowIfInvalid();
      return Provider.Decrypt(bytes, true);
    }

    public void Dispose() => Provider.Dispose();
  }

  public long SharedId => Data.SharedId;
  public long UserId => Data.UserId;
  public long UserAuthenticationId => Data.UserAuthenticationId;
  public byte[] PrivateIv => Data.PrivateIv;
  public byte[] EncryptedPrivateKey => Data.EncryptedPrivateKey;
  public byte[] PublicKey => Data.PublicKey;

  public Transformer GetTransformer(byte[]? hashCache = null)
  {
    ThrowIfInvalid();
    RSACryptoServiceProvider provider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KeyGenerator.KEY_SIZE
    };

    Transformer transform = new(this, provider);
    provider.ImportCspBlob(hashCache != null ? Aes.Create().CreateDecryptor(hashCache, PrivateIv).TransformFinalBlock(EncryptedPrivateKey) : PublicKey);
    return transform;
  }
}

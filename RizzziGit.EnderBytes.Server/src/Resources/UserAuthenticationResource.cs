using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed partial class UserAuthenticationResource(UserAuthenticationResource.ResourceManager manager, UserAuthenticationResource.ResourceData data) : Resource<UserAuthenticationResource.ResourceManager, UserAuthenticationResource.ResourceData, UserAuthenticationResource>(manager, data)
{
  public sealed record Token(UserAuthenticationResource UserAuthentication, byte[] PayloadHash)
  {
    public long UserId => UserAuthentication.UserId;

    public bool IsValid => UserAuthentication.IsValid;
    public void ThrowIfInvalid() => UserAuthentication.ThrowIfInvalid();

    public byte[] Encrypt(byte[] bytes) => UserAuthentication.Encrypt(bytes);
    public byte[] Decrypt(byte[] bytes) => UserAuthentication.Decrypt(bytes, PayloadHash);
  }

  public enum UserAuthenticationType { Password }

  private const string NAME = "UserAuthentication";
  private const int VERSION = 1;

  public new sealed partial class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceManager
  {
    private const string COLUMN_USER_ID = "UserId";
    private const string COLUMN_TYPE = "Type";

    private const string COLUMN_SALT = "Salt";
    private const string COLUMN_ITERATIONS = "Iterations";

    private const string COLUMN_CHALLENGE_IV = "ChallengeIv";
    private const string COLUMN_CHALLENGE_BYTES = "ChallengeBytes";
    private const string COLUMN_CHALLENGE_ENCRYPTED_BYTES = "ChallengeEncryptedBytes";

    private const string COLUMN_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    private const string COLUMN_ENCRYPTED_PRIVATE_KEY_IV = "EncryptedPrivateKeyIv";
    private const string COLUMN_PUBLIC_KEY = "PublicKey";

    private const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", resource.Id));
    }

    protected override UserAuthenticationResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_USER_ID)),
      (UserAuthenticationType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE)),

      reader.GetBytes(reader.GetOrdinal(COLUMN_SALT)),
      reader.GetInt32(reader.GetOrdinal(COLUMN_ITERATIONS)),

      reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_IV)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_BYTES)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_ENCRYPTED_BYTES)),

      reader.GetBytes(reader.GetOrdinal(COLUMN_ENCRYPTED_PRIVATE_KEY)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_ENCRYPTED_PRIVATE_KEY_IV)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_PUBLIC_KEY))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} integer not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_SALT} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ITERATIONS} integer not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_BYTES} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_ENCRYPTED_BYTES} blob not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PUBLIC_KEY} blob not null;");

        SqlNonQuery(transaction, $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});");
      }
    }

    public Token CreatePassword(ResourceService.Transaction transaction, UserResource user, Token existing, string password) => Create(transaction, user, existing, UserAuthenticationType.Password, Encoding.UTF8.GetBytes(password));
    public Token CreatePassword(ResourceService.Transaction transaction, UserResource user, string password) => Create(transaction, user, UserAuthenticationType.Password, Encoding.UTF8.GetBytes(password));

    public Token Create(ResourceService.Transaction transaction, UserResource user, Token existing, UserAuthenticationType type, byte[] payload)
    {
      user.ThrowIfInvalid();
      existing.UserAuthentication.ThrowIfInvalid();

      byte[] privateKey = existing.UserAuthentication.GetPrivateKey(existing.PayloadHash);
      byte[] publicKey = existing.UserAuthentication.PublicKey;

      byte[] salt = RandomNumberGenerator.GetBytes(16);
      int iterations = RandomNumberGenerator.GetInt32(1000, 10000);
      byte[] payloadHash;
      {
        using Rfc2898DeriveBytes rfc2898DeriveBytes = new(payload, salt, iterations, HashAlgorithmName.SHA256);
        payloadHash = rfc2898DeriveBytes.GetBytes(32);
      }

      byte[] challengeIv = RandomNumberGenerator.GetBytes(16);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(16);
      byte[] challengeEncryptedBytes = AesEncrypt(payloadHash, challengeIv, challengeBytes);

      byte[] encryptedPrivateKeyIv = RandomNumberGenerator.GetBytes(16);
      byte[] encryptedPrivateKey = AesEncrypt(payloadHash, encryptedPrivateKeyIv, privateKey);

      return new(Insert(transaction, new(
        (COLUMN_USER_ID, user.Id),
        (COLUMN_TYPE, (byte)type),

        (COLUMN_SALT, salt),
        (COLUMN_ITERATIONS, iterations),

        (COLUMN_CHALLENGE_IV, challengeIv),
        (COLUMN_CHALLENGE_BYTES, challengeBytes),
        (COLUMN_CHALLENGE_ENCRYPTED_BYTES, challengeEncryptedBytes),

        (COLUMN_ENCRYPTED_PRIVATE_KEY, encryptedPrivateKey),
        (COLUMN_ENCRYPTED_PRIVATE_KEY_IV, encryptedPrivateKeyIv),
        (COLUMN_PUBLIC_KEY, publicKey)
      )), payloadHash);
    }

    public Token Create(ResourceService.Transaction transaction, UserResource user, UserAuthenticationType type, byte[] payload)
    {
      user.ThrowIfInvalid();

      if (Count(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id)) != 0)
      {
        throw new InvalidOperationException("Must use an existing rsa key.");
      }

      byte[] salt = RandomNumberGenerator.GetBytes(16);
      int iterations = RandomNumberGenerator.GetInt32(1000, 10000);
      byte[] payloadHash;
      {
        using Rfc2898DeriveBytes rfc2898DeriveBytes = new(payload, salt, iterations, HashAlgorithmName.SHA256);
        payloadHash = rfc2898DeriveBytes.GetBytes(32);
      }

      byte[] challengeIv = RandomNumberGenerator.GetBytes(16);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(64);
      byte[] challengeEncryptedBytes = AesEncrypt(payloadHash, challengeIv, challengeBytes);

      (byte[] privateKey, byte[] publicKey) = Service.Server.KeyService.GetNewRsaKeyPair();
      byte[] encryptedPrivateKeyIv = RandomNumberGenerator.GetBytes(16);
      byte[] encryptedPrivateKey = AesEncrypt(payloadHash, encryptedPrivateKeyIv, privateKey);

      return new(Insert(transaction, new(
        (COLUMN_USER_ID, user.Id),
        (COLUMN_TYPE, (byte)type),

        (COLUMN_SALT, salt),
        (COLUMN_ITERATIONS, iterations),

        (COLUMN_CHALLENGE_IV, challengeIv),
        (COLUMN_CHALLENGE_BYTES, challengeBytes),
        (COLUMN_CHALLENGE_ENCRYPTED_BYTES, challengeEncryptedBytes),

        (COLUMN_ENCRYPTED_PRIVATE_KEY, encryptedPrivateKey),
        (COLUMN_ENCRYPTED_PRIVATE_KEY_IV, encryptedPrivateKeyIv),
        (COLUMN_PUBLIC_KEY, publicKey)
      )), payloadHash);
    }

    public IEnumerable<UserAuthenticationResource> List(ResourceService.Transaction transaction, UserResource user, LimitClause? limitClause = null, OrderByClause? orderByClause = null) => Select(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id), limitClause, orderByClause);

    public Token? GetByPayload(ResourceService.Transaction transaction, UserResource user, byte[] payload, UserAuthenticationType type)
    {
      foreach (UserAuthenticationResource userAuthentication in Select(transaction,
        new WhereClause.Nested("and",
          new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id),
          new WhereClause.CompareColumn(COLUMN_TYPE, "=", (byte)type)
        )
      ))
      {
        try { return new(userAuthentication, userAuthentication.GetPayloadHash(payload)); } catch { }
      }

      return null;
    }

    public override bool Delete(ResourceService.Transaction transaction, UserAuthenticationResource userAuthentication)
    {
      userAuthentication.ThrowIfInvalid();

      if (Count(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", userAuthentication.UserId)) < 2)
      {
        throw new InvalidOperationException("Must have at least two user authentications before deleting one.");
      }

      return base.Delete(transaction, userAuthentication);
    }
  }

  public new sealed partial record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long UserId,
    UserAuthenticationType Type,

    byte[] Salt,
    int Iterations,

    byte[] ChallengeIv,
    byte[] ChallengeBytes,
    byte[] ChallengeEncryptedBytes,

    byte[] EncryptedPrivateKey,
    byte[] EncryptedPrivateKeyIv,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceData(Id, CreateTime, UpdateTime);

  public static byte[] AesEncrypt(byte[] key, byte[] iv, byte[] bytes)
  {
    using Aes aes = Aes.Create();
    using ICryptoTransform cryptoTransform = aes.CreateEncryptor(key, iv);

    return cryptoTransform.TransformFinalBlock(bytes);
  }

  public static byte[] AesDecrypt(byte[] key, byte[] iv, byte[] bytes)
  {
    using Aes aes = Aes.Create();
    using ICryptoTransform cryptoTransform = aes.CreateDecryptor(key, iv);

    return cryptoTransform.TransformFinalBlock(bytes);
  }

  public long UserId => Data.UserId;
  public UserAuthenticationType Type => Data.Type;

  private byte[] Salt => Data.Salt;
  private int Iterations => Data.Iterations;

  private byte[] ChallengeIv => Data.ChallengeIv;
  private byte[] ChallengeBytes => Data.ChallengeBytes;
  private byte[] ChallengeEncryptedBytes => Data.ChallengeEncryptedBytes;

  private byte[] EncryptedPrivateKey => Data.EncryptedPrivateKey;
  private byte[] EncryptedPrivateKeyIv => Data.EncryptedPrivateKeyIv;
  private byte[] PublicKey => Data.PublicKey;

  private byte[]? PrivateKey;
  private byte[] GetPrivateKey(byte[] payloadHash)
  {
    lock (this)
    {
      return PrivateKey ??= AesDecrypt(payloadHash, EncryptedPrivateKeyIv, EncryptedPrivateKey);
    }
  }

  private RSACryptoServiceProvider? CryptoServiceProvider;
  private Token? TokenCache;

  private byte[] GetPayloadHash(byte[] payload)
  {
    using Rfc2898DeriveBytes rfc2898DeriveBytes = new(payload, Salt, Iterations, HashAlgorithmName.SHA256);
    byte[] payloadHash = rfc2898DeriveBytes.GetBytes(32);

    if (!AesEncrypt(payloadHash, ChallengeIv, ChallengeBytes).SequenceEqual(ChallengeEncryptedBytes))
    {
      throw new ArgumentException("Invalid payload.", nameof(payload));
    }

    return payloadHash;
  }

  private static RSACryptoServiceProvider GetRSACryptoServiceProvider(byte[] cspBlob)
  {
    RSACryptoServiceProvider cryptoServiceProvider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KeyService.KEY_SIZE
    };

    cryptoServiceProvider.ImportCspBlob(cspBlob);
    return cryptoServiceProvider;
  }

  private byte[] Decrypt(byte[] bytes, byte[] payloadHash)
  {
    lock (this)
    {
      if (CryptoServiceProvider?.PublicOnly != false)
      {
        byte[] privateKey = GetPrivateKey(payloadHash);

        CryptoServiceProvider?.Dispose();
        CryptoServiceProvider = GetRSACryptoServiceProvider(privateKey);
      }

      return CryptoServiceProvider.Decrypt(bytes, false);
    }
  }

  public byte[] Encrypt(byte[] bytes)
  {
    lock (this)
    {
      return (CryptoServiceProvider ??= GetRSACryptoServiceProvider(PublicKey)).Encrypt(bytes, false);
    }
  }

  public Token GetTokenByPayload(byte[] payload)
  {
    lock (this)
    {
      byte[] payloadHash = GetPayloadHash(payload);

      return TokenCache ??= new(this, payloadHash);
    }
  }
}

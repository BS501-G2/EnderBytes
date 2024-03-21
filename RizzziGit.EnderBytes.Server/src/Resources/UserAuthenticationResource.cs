using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;
using System.Diagnostics.CodeAnalysis;

public sealed partial class UserAuthenticationResource(UserAuthenticationResource.ResourceManager manager, UserAuthenticationResource.ResourceData data) : Resource<UserAuthenticationResource.ResourceManager, UserAuthenticationResource.ResourceData, UserAuthenticationResource>(manager, data)
{
  public sealed record UserAuthenticationToken(UserResource User, UserAuthenticationResource UserAuthentication, byte[] PayloadHash)
  {
    public long UserId => UserAuthentication.UserId;

    public bool IsValid => UserAuthentication.IsValid;
    public void ThrowIfInvalid() => UserAuthentication.ThrowIfInvalid();

    public byte[] Encrypt(byte[] bytes) => UserAuthentication.Encrypt(bytes);
    public byte[] Decrypt(byte[] bytes) => UserAuthentication.Decrypt(bytes, PayloadHash);

    public bool TryEnter<T>(Func<T> func, [NotNullWhen(true)] out T result)
    {

      lock (this)
      {
        lock (UserAuthentication)
        {
          if (IsValid)
          {
            result = func();
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
            return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
          }
        }
      }

#pragma warning disable CS8601 // Possible null reference assignment.
      result = default;
#pragma warning restore CS8601 // Possible null reference assignment.
      return false;
    }

    public bool TryEnter(Action action)
    {
      lock (this)
      {
        lock (UserAuthentication)
        {
          if (IsValid)
          {
            action();

            return true;
          }
        }
      }

      return false;
    }

    public T Enter<T>(Func<T> func)
    {
      lock (this)
      {
        lock (UserAuthentication)
        {
          ThrowIfInvalid();

          return func();
        }
      }
    }

    public void Enter(Action action)
    {
      lock (this)
      {
        lock (UserAuthentication)
        {
          ThrowIfInvalid();

          action();
        }
      }
    }
  }

  public enum UserAuthenticationType { Password }

  public const string NAME = "UserAuthentication";
  public const int VERSION = 1;

  public new sealed partial class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceManager
  {
    public const string COLUMN_USER_ID = "UserId";
    public const string COLUMN_TYPE = "Type";

    public const string COLUMN_SALT = "Salt";
    public const string COLUMN_ITERATIONS = "Iterations";

    public const string COLUMN_CHALLENGE_IV = "ChallengeIv";
    public const string COLUMN_CHALLENGE_BYTES = "ChallengeBytes";
    public const string COLUMN_CHALLENGE_ENCRYPTED_BYTES = "ChallengeEncryptedBytes";

    public const string COLUMN_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    public const string COLUMN_ENCRYPTED_PRIVATE_KEY_IV = "EncryptedPrivateKeyIv";
    public const string COLUMN_PUBLIC_KEY = "PublicKey";

    public const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id), cancellationToken);
    }

    protected override UserAuthenticationResource NewResource(ResourceData data) => new(this, data);

    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
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

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_USER_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_SALT} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ITERATIONS} bigint not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_BYTES} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_CHALLENGE_ENCRYPTED_BYTES} blob not null;");

        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PUBLIC_KEY} blob not null;");

        SqlNonQuery(transaction, $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});");
      }
    }

    public UserAuthenticationToken CreatePassword(ResourceService.Transaction transaction, UserResource user, UserAuthenticationToken existing, string password) => Create(transaction, user, existing, UserAuthenticationType.Password, Encoding.UTF8.GetBytes(password));
    public UserAuthenticationToken CreatePassword(ResourceService.Transaction transaction, UserResource user, string password, byte[] privateKey, byte[] publicKey) => Create(transaction, user, UserAuthenticationType.Password, Encoding.UTF8.GetBytes(password), privateKey, publicKey);

    private UserAuthenticationToken Create(ResourceService.Transaction transaction, UserResource user, UserAuthenticationToken existing, UserAuthenticationType type, byte[] payload)
    {
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

      return new(user, InsertAndGet(transaction, new(
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

    private UserAuthenticationToken Create(ResourceService.Transaction transaction, UserResource user, UserAuthenticationType type, byte[] payload, byte[] privateKey, byte[] publicKey)
    {
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

      byte[] encryptedPrivateKeyIv = RandomNumberGenerator.GetBytes(16);
      byte[] encryptedPrivateKey = AesEncrypt(payloadHash, encryptedPrivateKeyIv, privateKey);

      return new(user, InsertAndGet(transaction, new(
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

    public UserAuthenticationToken? GetByPayload(ResourceService.Transaction transaction, UserResource user, byte[] payload, UserAuthenticationType type)
    {
      lock (user)
      {
        user.ThrowIfInvalid();

        foreach (UserAuthenticationResource userAuthentication in Select(transaction,
          new WhereClause.Nested("and",
            new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id),
            new WhereClause.CompareColumn(COLUMN_TYPE, "=", (byte)type)
          )
        ))
        {
          try { return new(user, userAuthentication, userAuthentication.GetPayloadHash(payload)); } catch { }
        }

        return null;
      }
    }

    public override bool Delete(ResourceService.Transaction transaction, UserAuthenticationResource userAuthentication, CancellationToken cancellationToken = default)
    {
      lock (userAuthentication)
      {
        userAuthentication.ThrowIfInvalid();

        if (Count(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", userAuthentication.UserId), cancellationToken) < 2)
        {
          throw new InvalidOperationException("Must have at least two user authentications before deleting one.");
        }

        return base.Delete(transaction, userAuthentication, cancellationToken);
      }
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
  private RSACryptoServiceProvider? CryptoServiceProvider;
  private UserAuthenticationToken? TokenCache;

  ~UserAuthenticationResource() => CryptoServiceProvider?.Dispose();

  private byte[] GetPrivateKey(byte[] payloadHash)
  {
    lock (this)
    {
      return PrivateKey ??= AesDecrypt(payloadHash, EncryptedPrivateKeyIv, EncryptedPrivateKey);
    }
  }

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

  private RSACryptoServiceProvider GetRSACryptoServiceProvider(byte[] cspBlob)
  {
    RSACryptoServiceProvider cryptoServiceProvider = Manager.Service.Server.KeyService.GetRsaCryptoServiceProvider(cspBlob);

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

  public bool TryGetTokenByPayload(UserResource user, byte[] payload, [NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken)
  {
    userAuthenticationToken = null;

    lock (this)
    {
      if (!user.IsValid || user.Id != UserId)
      {
        return false;
      }

      byte[] payloadHash = GetPayloadHash(payload);

      userAuthenticationToken = TokenCache ??= new(user, this, payloadHash);
      return true;
    }
  }
}

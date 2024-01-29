using System.Security.Cryptography;
using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed partial class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  private const string NAME = "Key";
  private const int VERSION = 1;

  public new sealed partial class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction,  resource) =>
      {

      };
    }

    private const string COLUMN_SHARED_ID = "SharedId";
    private const string COLUMN_TARGET_USER_ID = "TargetUserId";
    private const string COLUMN_PRIVATE_KEY = "PrivateKey";
    private const string COLUMN_PUBLIC_KEY = "PublicKey";

    private const string INDEX_SHARED_ID = $"Index_{NAME}_{COLUMN_SHARED_ID}";
    private const string INDEX_TARGET_USER_ID = $"Index_{NAME}_{COLUMN_TARGET_USER_ID}";

    protected override KeyResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_SHARED_ID)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TARGET_USER_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_PRIVATE_KEY)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_PUBLIC_KEY))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_SHARED_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_USER_ID} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PRIVATE_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PUBLIC_KEY} blob not null;");

        SqlNonQuery(transaction, $"create index {INDEX_SHARED_ID} on {NAME}({COLUMN_SHARED_ID});");
        SqlNonQuery(transaction, $"create index {INDEX_TARGET_USER_ID} on {NAME}({COLUMN_TARGET_USER_ID});");
      }
    }

    public KeyResource? Get(ResourceService.Transaction transaction, long sharedId, UserAuthenticationResource? userAuthentication) => SelectFirst(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_SHARED_ID, "=", sharedId),
      new WhereClause.CompareColumn(COLUMN_TARGET_USER_ID, "=", userAuthentication?.UserId)
    ));

    public IEnumerable<KeyResource> ListBySharedId(ResourceService.Transaction transaction, long sharedId) => Select(transaction, new WhereClause.CompareColumn(COLUMN_SHARED_ID, "=", sharedId));
    public IEnumerable<KeyResource> ListByTargetUserId(ResourceService.Transaction transaction, long? targetUserId) => Select(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_USER_ID, "=", targetUserId));

    private long NewSharedId(ResourceService.Transaction transaction)
    {
      long sharedId;

      do
      {
        sharedId = Random.Shared.NextInt64();
      }
      while (Exists(transaction, new WhereClause.CompareColumn(INDEX_SHARED_ID, "=", sharedId)));

      return sharedId;
    }

    public KeyResource CopyFrom(ResourceService.Transaction transaction, long sharedId, (UserAuthenticationResource UserAuthentication, byte[] PayloadHash)? from, UserAuthenticationResource? newUserAuthentication)
    {
      newUserAuthentication?.ThrowIfInalid();

      KeyResource key = SelectOne(transaction,
        new WhereClause.Nested("and",
          new WhereClause.CompareColumn(COLUMN_SHARED_ID, "=", sharedId),
          new WhereClause.CompareColumn(COLUMN_TARGET_USER_ID, "=", from?.UserAuthentication.UserId)
        )
      ) ?? throw new ArgumentException("Shared id or target user is invalid.");

      byte[] privateKey = from != null
        ? from.Value.UserAuthentication.Decrypt(key.PrivateKey, from.Value.PayloadHash)
        : key.PrivateKey;

      return Insert(transaction, new(
        (COLUMN_SHARED_ID, NewSharedId(transaction)),
        (COLUMN_TARGET_USER_ID, newUserAuthentication?.UserId),
        (COLUMN_PRIVATE_KEY, newUserAuthentication?.Encrypt(privateKey) ?? privateKey),
        (COLUMN_PUBLIC_KEY, key.PublicKey)
      ));
    }

    public KeyResource Create(ResourceService.Transaction transaction, UserAuthenticationResource? userAuthentication)
    {
      userAuthentication?.ThrowIfInalid();

      (byte[] privateKey, byte[] publicKey) = Service.Server.KeyService.GetNewRsaKeyPair();
      return Insert(transaction, new(
        (COLUMN_SHARED_ID, NewSharedId(transaction)),
        (COLUMN_TARGET_USER_ID, userAuthentication?.UserId),
        (COLUMN_PRIVATE_KEY, userAuthentication?.Encrypt(privateKey) ?? privateKey),
        (COLUMN_PUBLIC_KEY, publicKey)
      ));
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SharedId,
    long? TargetUserId,
    byte[] PrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime);

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

  public long SharedId => Data.SharedId;
  public long? TargetUserId => Data.TargetUserId;

  private byte[] PrivateKey => Data.PrivateKey;
  private byte[] PublicKey => Data.PublicKey;

  private RSACryptoServiceProvider? CryptoServiceProvider;

  private byte[] Decrypt(byte[] bytes, byte[] privateKey)
  {
    lock (this)
    {
      if (CryptoServiceProvider?.PublicOnly != false)
      {
        CryptoServiceProvider?.Dispose();
        CryptoServiceProvider = GetRSACryptoServiceProvider(privateKey);
      }

      return CryptoServiceProvider.Decrypt(bytes, false);
    }
  }

  public byte[] Decrypt(byte[] bytes)
  {
    if (TargetUserId != null)
    {
      throw new InvalidOperationException("Requires user authentication to decrypt using this key, or find a copy of this key that does not require user authentication.");
    }

    return Decrypt(bytes, PrivateKey);
  }

  public byte[] Decrypt(byte[] bytes, UserAuthenticationResource userAuthentication, byte[] payloadHash)
  {
    if (TargetUserId == null)
    {
      return Decrypt(bytes, PrivateKey);
    }

    if (userAuthentication.UserId != TargetUserId)
    {
      throw new ArgumentException("Invalid user authentication.", nameof(userAuthentication));
    }

    return Decrypt(bytes, userAuthentication.Decrypt(PrivateKey, payloadHash));
  }

  public byte[] Encrypt(byte[] bytes)
  {
    lock (this)
    {
      return (CryptoServiceProvider ??= GetRSACryptoServiceProvider(PublicKey)).Encrypt(bytes, false);
    }
  }
}

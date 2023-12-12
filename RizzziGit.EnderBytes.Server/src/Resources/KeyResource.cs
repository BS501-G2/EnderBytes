using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Keys;

public sealed class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    private const string NAME = "Key";
    private const int VERSION = 1;

    private const string KEY_SHARED_ID = "SharedId";
    private const string KEY_USER_KEY_SHARED_ID = "UserKeySharedId";
    private const string KEY_USER_ID = "UserId";
    private const string KEY_PRIVATE_KEY = "PrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    public ResourceManager(IMainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override KeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_SHARED_ID],
      reader[KEY_USER_KEY_SHARED_ID] is null ? null : (long)reader[KEY_USER_KEY_SHARED_ID],
      reader[KEY_USER_ID] is null ? null : (long)reader[KEY_USER_ID],
      (byte[])reader[KEY_PRIVATE_KEY],
      (byte[])reader[KEY_PUBLIC_KEY]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_SHARED_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_KEY_SHARED_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_KEY} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_KEY} blob not null");
      }
    }

    public KeyResource? GetBySharedId(DatabaseTransaction transaction, long sharedId, long? userKeySharedId)
    {
      return DbOnce(transaction, new()
      {
        { KEY_SHARED_ID, ("=", sharedId) },
        { KEY_USER_KEY_SHARED_ID, ("=", userKeySharedId) }
      });
    }

    public KeyResource Create(DatabaseTransaction transaction, UserKeyResource.Transformer? transformer, byte[] privateKey, byte[] publicKey)
    {
      long sharedId;
      do
      {
        sharedId = Random.Shared.NextInt64();
      }
      while (DbOnce(transaction, new() { { KEY_SHARED_ID, ("=", sharedId) } }) != null);

      return DbInsert(transaction, new()
      {
        { KEY_SHARED_ID, sharedId },
        { KEY_USER_KEY_SHARED_ID, transformer?.UserKey.SharedId },
        { KEY_USER_ID, transformer?.UserKey.UserId },
        { KEY_PRIVATE_KEY, transformer?.Encrypt(privateKey) ?? privateKey },
        { KEY_PUBLIC_KEY, publicKey }
      });
    }

    public KeyResource Create(
      DatabaseTransaction transaction,
      KeyResource existing,
      UserKeyResource.Transformer? fromTransformer,
      UserKeyResource.Transformer? toTransformer
    )
    {
      if (fromTransformer?.UserKey.SharedId != existing.UserKeySharedId)
      {
        throw new ArgumentException("Invalid user key shared ID.");
      }

      byte[] privateKey = existing.DecryptPrivateKey(fromTransformer);
      return DbInsert(transaction, new()
      {
        { KEY_SHARED_ID, existing.SharedId },
        { KEY_USER_KEY_SHARED_ID, toTransformer?.UserKey.SharedId },
        { KEY_PRIVATE_KEY, toTransformer?.Encrypt(privateKey) ?? privateKey },
        { KEY_PUBLIC_KEY, existing.PublicKey }
      });
    }

    public IEnumerable<KeyResource> StreamKeysByUser(DatabaseTransaction transaction, UserResource user) => DbStream(transaction, new()
    {
      { KEY_USER_ID, ("=", user.Id) }
    });
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SharedId,
    long? UserKeySharedId,
    long? UserId,
    byte[] PrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public sealed class Transformer(KeyResource key, RSACryptoServiceProvider serviceProvider) : IDisposable
  {
    public readonly KeyResource Key = key;

    private readonly RSACryptoServiceProvider Provider = serviceProvider;

    public byte[] Encrypt(byte[] bytes)
    {
      Key.ThrowIfInvalid();
      return Provider.Encrypt(bytes, true);
    }

    public byte[] Decrypt(byte[] bytes)
    {
      Key.ThrowIfInvalid();
      return Provider.Decrypt(bytes, true);
    }

    public void Dispose() => Provider.Dispose();
  }

  public long SharedId => Data.SharedId;
  public long? UserKeySharedId => Data.UserKeySharedId;
  public long? UserId => Data.UserId;
  public byte[] PrivateKey => Data.PrivateKey;
  public byte[] PublicKey => Data.PublicKey;

  public byte[] DecryptPrivateKey(UserKeyResource.Transformer? transformer)
  {
    if (UserKeySharedId != null)
    {
      if (transformer == null)
      {
        throw new InvalidOperationException("Requires key transformer.");
      }
      else if (transformer?.UserKey.SharedId != UserKeySharedId)
      {
        throw new InvalidOperationException("Requires matching user key shared id.");
      }

      return transformer.Decrypt(PrivateKey);
    }

    return PrivateKey;
  }

  public Transformer GetTransformer(UserKeyResource.Transformer? userKeyTransformer = null)
  {
    ThrowIfInvalid();

    RSACryptoServiceProvider provider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KeyGenerator.KEY_SIZE
    };

    Transformer keyTransformer = new(this, provider);
    provider.ImportCspBlob(UserKeySharedId == null ? PrivateKey : userKeyTransformer == null ? PublicKey : userKeyTransformer.Decrypt(PrivateKey));
    return keyTransformer;
  }
}

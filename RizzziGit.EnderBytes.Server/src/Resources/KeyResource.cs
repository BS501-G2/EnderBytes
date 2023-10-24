using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    public const string NAME = "Key";
    public const int VERSION = 1;

    private const string KEY_USER_AUTHENTICATION_ID = "UserAuthenticationId";
    private const string KEY_PRIVATE_IV = "PrivateIv";
    private const string KEY_PRIVATE_KEY = "PrivateKey";
    private const string KEY_PUBLIC_KEY = "PublicKey";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (long)reader[KEY_USER_AUTHENTICATION_ID],
      (byte[])reader[KEY_PRIVATE_IV],
      (byte[])reader[KEY_PRIVATE_KEY],
      (byte[])reader[KEY_PUBLIC_KEY]
    );

    protected override KeyResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_AUTHENTICATION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_IV} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_KEY} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_KEY} blob not null;");
      }
    }

    public KeyResource Create(
      DatabaseTransaction transaction,
      UserAuthenticationResource userAuthentication,
      byte[] hashCache
    )
    {
      byte[] privateIv = new byte[16];
      using var rsa = new RSACryptoServiceProvider()
      {
        PersistKeyInCsp = false,
        KeySize = 1024
      };
      byte[] privateKey = rsa.ExportRSAPrivateKey();
      byte[] publicKey = rsa.ExportRSAPublicKey();

      byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hashCache, privateIv).TransformFinalBlock(privateKey, 0, privateKey.Length);
      return DbInsert(transaction, new()
      {
        { KEY_USER_AUTHENTICATION_ID, userAuthentication.Id },
        { KEY_PRIVATE_IV, privateIv },
        { KEY_PRIVATE_KEY, encryptedPrivateKey },
        { KEY_PUBLIC_KEY, publicKey }
      });
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserAuthenticationId,
    byte[] PrivateIv,
    byte[] PrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_USER_AUTHENTICATION_ID = "userAuthenticationId";
    public const string KEY_PRIVATE_IV = "privateIv";
    public const string KEY_PRIVATE_KEY = "privateKey";
    public const string KEY_PUBLIC_KEY = "publicKey";

    [JsonPropertyName(KEY_USER_AUTHENTICATION_ID)] public long UserAuthenticationId = UserAuthenticationId;
    [JsonPropertyName(KEY_PRIVATE_IV)] public byte[] PrivateIv = PrivateIv;
    [JsonPropertyName(KEY_PRIVATE_IV)] public byte[] PrivateKey = PrivateKey;
    [JsonPropertyName(KEY_PRIVATE_IV)] public byte[] PublicKey = PublicKey;
  }

  public long UserAuthenticationId => Data.UserAuthenticationId;
  public byte[] PrivateIV => Data.PrivateIv;
  public byte[] PrivateKey => Data.PrivateKey;
  public byte[] PublicKey => Data.PublicKey;
}

using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    private const string NAME = "Name";
    private const int VERSION = 1;

    private const string KEY_USER_AUTHENTICATION_ID = "UserAuthenticationID";
    private const string KEY_PRIVATE_PAYLOAD = "PrivatePayload";
    private const string KEY_PRIVATE_IV = "PrivateIV";
    private const string KEY_PUBLIC_PAYLOAD = "PublicPayload";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.UserAuthentications.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_USER_AUTHENTICATION_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_USER_AUTHENTICATION_ID],
      (byte[])reader[KEY_PRIVATE_PAYLOAD],
      (byte[])reader[KEY_PRIVATE_IV],
      (byte[])reader[KEY_PUBLIC_PAYLOAD]
    );

    protected override KeyResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_AUTHENTICATION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_PAYLOAD} blob not null");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE_IV} blob not null");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC_PAYLOAD} blob not null");
      }
    }

    public KeyResource Create(DatabaseTransaction transaction, UserAuthenticationResource userAuthentication, byte[] hashCache)
    {
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
        { KEY_USER_AUTHENTICATION_ID, userAuthentication.Id },
        { KEY_PRIVATE_PAYLOAD, encryptedPrivateKey },
        { KEY_PRIVATE_IV, privateIv },
        { KEY_PUBLIC_PAYLOAD, publicKey }
      });
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserAuthenticationID,
    byte[] PrivatePayload,
    byte[] PrivateIv,
    byte[] PublicPayload
  ) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long UserAuthenticationID => Data.UserAuthenticationID;
  public byte[] PrivatePayload => Data.PrivatePayload;
  public byte[] PrivateIv => Data.PrivateIv;
  public byte[] PublicPayload => Data.PublicPayload;

  public byte[] Encrypt(byte[] bytes)
  {
    using var rsa = new RSACryptoServiceProvider()
    {
      PersistKeyInCsp = false,
      KeySize = 1024
    };
    rsa.ImportRSAPublicKey(PublicPayload, out _);
    return rsa.Encrypt(bytes, false);
  }

  public byte[] Decrypt(byte[] bytes, byte[] hashCache)
  {
    using var rsa = new RSACryptoServiceProvider()
    {
      PersistKeyInCsp = false,
      KeySize = 1024
    };

    rsa.ImportRSAPrivateKey(Aes.Create().CreateDecryptor(hashCache, PrivateIv).TransformFinalBlock(PrivatePayload, 0, PrivatePayload.Length), out _);
    return rsa.Decrypt(bytes, false);
  }
}

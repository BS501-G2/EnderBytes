using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Extensions;
using Collections;

public sealed class BlobFileKeyResource(BlobFileKeyResource.ResourceManager manager, BlobFileKeyResource.ResourceData data) : Resource<BlobFileKeyResource.ResourceManager, BlobFileKeyResource.ResourceData, BlobFileKeyResource>(manager, data)
{
  public const int BUFFER_SIZE = 512;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileKeyResource>.ResourceManager
  {
    private const string NAME = "BlobFileKey";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_USER_AUTHENTICATION_ID = "UserAuthenticationId";
    private const string KEY_PUBLIC = "PublicKey";
    private const string KEY_PRIVATE = "PrivateKey";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      RNG = RandomNumberGenerator.Create();

      Main.Files.OnResourceDelete += (transaction, resource) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      });
    }

    private readonly RandomNumberGenerator RNG;

    protected override BlobFileKeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_USER_AUTHENTICATION_ID],
      (byte[])reader[KEY_PUBLIC],
      (byte[])reader[KEY_PRIVATE]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_AUTHENTICATION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PUBLIC} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PRIVATE} blob not null;");
      }
    }

    public BlobFileKeyResource Create(DatabaseTransaction transaction, BlobFileResource file, UserAuthenticationResource userAuthentication, byte[] hashCache)
    {
      using var rsa = new RSACryptoServiceProvider()
      {
        PersistKeyInCsp = false,
        KeySize = 8 * BUFFER_SIZE
      };

      try
      {
        Console.WriteLine("test");
        byte[] iv = RNG.GetBytes(16);
        byte[] privateKey = rsa.ExportCspBlob(true);
        Console.WriteLine("test");

        return DbInsert(transaction, new()
        {
          { KEY_FILE_ID, file.Id },
          { KEY_USER_AUTHENTICATION_ID, userAuthentication.Id },
          { KEY_PUBLIC, rsa.ExportCspBlob(false) },
          {
            KEY_PRIVATE, (byte[])[
              .. privateKey,
              .. Aes.Create().CreateEncryptor(hashCache, iv).TransformFinalBlock(privateKey, 0, privateKey.Length)
            ]
          }
        });
      }
      finally
      {
        rsa.Clear();
      }
    }

    public BlobFileKeyResource Create(DatabaseTransaction transaction, BlobFileResource file, UserAuthenticationResource userAuthentication, byte[] hashCache, BlobFileKeyResource fromKey, byte[] fromHashCache)
    {
      using var fromRsa = new RSACryptoServiceProvider()
      {
        PersistKeyInCsp = false,
        KeySize = 8 * BUFFER_SIZE
      };

      using var rsa = new RSACryptoServiceProvider()
      {
        PersistKeyInCsp = false,
        KeySize = 8 * BUFFER_SIZE
      };

      try
      {
        byte[] iv = fromKey.Private[0..16];
        byte[] privateKey = Aes.Create().CreateDecryptor(fromHashCache, iv).TransformFinalBlock(fromKey.Private, 16, fromKey.Private.Length - 16);

        return DbInsert(transaction, new()
        {
          { KEY_FILE_ID, file.Id },
          { KEY_USER_AUTHENTICATION_ID, userAuthentication.Id },
          { KEY_PUBLIC, fromKey.Public },
          {
            KEY_PRIVATE, (byte[])[
              .. privateKey,
              .. Aes.Create().CreateEncryptor(hashCache, iv).TransformFinalBlock(privateKey, 0, privateKey.Length)
            ]
          }
        });
      }
      finally
      {
        fromRsa.Clear();
        rsa.Clear();
      }
    }

    public BlobFileKeyResource? Get(DatabaseTransaction transaction, BlobFileResource file, UserAuthenticationResource userAuthentication)
    {
      foreach (BlobFileKeyResource fileKey in DbStream(transaction, new()
      {
        { KEY_FILE_ID, ("=", file.Id, null) },
        { KEY_USER_AUTHENTICATION_ID, ("=", userAuthentication.Id, null) }
      }, (1, null)))
      {
        return fileKey;
      }

      return null;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long UserAuthenticationId,
    byte[] Public,
    byte[] Private
  ) : Resource<ResourceManager, ResourceData, BlobFileKeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  ~BlobFileKeyResource()
  {
    foreach (KeyValuePair<ResourceData, RSACryptoServiceProvider> entry in PrivateRSA)
    {
      var (_, rsa) = entry;

      rsa.Clear();
      rsa.Dispose();
    }

    foreach (KeyValuePair<ResourceData, RSACryptoServiceProvider> entry in PublicRSA)
    {
      var (_, rsa) = entry;

      rsa.Clear();
      rsa.Dispose();
    }

    PrivateRSA.Clear();
    PublicRSA.Clear();
  }

  public long FileId => Data.FileId;
  public long UserAuthenticationId => Data.UserAuthenticationId;
  public byte[] Public => Data.Public;
  public byte[] Private => Data.Private;

  private readonly Dictionary<ResourceData, RSACryptoServiceProvider> PrivateRSA = [];
  private readonly Dictionary<ResourceData, RSACryptoServiceProvider> PublicRSA = [];

  private RSACryptoServiceProvider GetPrivateRSA(byte[] hashCache)
  {
    RSACryptoServiceProvider rsa;
    lock (PrivateRSA)
    {
      if (PrivateRSA.TryGetValue(Data, out var value))
      {
        rsa = value;
      }
      else
      {
        PrivateRSA.TryAdd(Data, rsa = new()
        {
          PersistKeyInCsp = false,
          KeySize = 8 * BUFFER_SIZE
        });

        rsa.ImportCspBlob(Aes.Create().CreateDecryptor(hashCache, Data.Private[0..16]).TransformFinalBlock(Data.Private, 16, Data.Private.Length - 16));
      }
    }

    return rsa;
  }

  private RSACryptoServiceProvider GetPublicRSA()
  {
    RSACryptoServiceProvider rsa;
    lock (PublicRSA)
    {
      if (PublicRSA.TryGetValue(Data, out var value))
      {
        rsa = value;
      }
      else
      {
        PublicRSA.TryAdd(Data, rsa = new()
        {
          PersistKeyInCsp = false,
          KeySize = 8 * BUFFER_SIZE
        });

        rsa.ImportCspBlob(Data.Public);
      }
    }

    return rsa;
  }

  public byte[] Decrypt(byte[] bytes, int offset, int length, byte[] hashCache) => Decrypt(bytes[offset..(offset + length)], hashCache);
  public byte[] Decrypt(byte[] bytes, byte[] hashCache) => GetPrivateRSA(hashCache).Decrypt(bytes, true);

  public byte[] Encrypt(byte[] bytes, int offset, int length) => Encrypt(bytes[offset..(offset + length)]);
  public byte[] Encrypt(byte[] bytes) => GetPublicRSA().Encrypt(bytes, true);
}

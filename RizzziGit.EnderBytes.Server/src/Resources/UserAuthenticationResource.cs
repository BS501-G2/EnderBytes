using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Extensions;

public enum UserAuthenticationType : byte
{
  Password
}

public sealed class UserAuthenticationResource(UserAuthenticationResource.ResourceManager manager, UserAuthenticationResource.ResourceData data) : Resource<UserAuthenticationResource.ResourceManager, UserAuthenticationResource.ResourceData, UserAuthenticationResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Users.OnResourceDelete(async (transaction, resource, cancellationToken) =>
      {
        await DbDelete(transaction, new() {
          { KEY_USER_ID, ("=", resource.Id, null) }
        }, cancellationToken);
      });
    }

    private static readonly Regex ValidPasswordRegex = new("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[\\W_])[a-zA-Z0-9\\W_]{8,64}$");

    public const string NAME = "UserAuthentication";
    public const int VERSION = 1;

    private const string KEY_USER_ID = "UserID";
    private const string KEY_TYPE = "Type";
    private const string KEY_ITERATIONS = "Iterations";
    private const string KEY_SALT = "Salt";
    private const string KEY_IV = "IV";
    private const string KEY_CHALLENGE_BYTES = "ChallengeBytes";
    private const string KEY_ENCRYPTED_BYTES = "EncryptedBytes";

    private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_USER_ID}_{KEY_TYPE}";

    public readonly RandomNumberGenerator RNG = RandomNumberGenerator.Create();

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_USER_ID],
      (UserAuthenticationType)(long)reader[KEY_TYPE],
      (int)(long)reader[KEY_ITERATIONS],
      (byte[])reader[KEY_SALT],
      (byte[])reader[KEY_IV],
      (byte[])reader[KEY_CHALLENGE_BYTES],
      (byte[])reader[KEY_ENCRYPTED_BYTES]
    );

    protected override UserAuthenticationResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_ITERATIONS} integer not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_SALT} blob not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_IV} blob not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_CHALLENGE_BYTES} blob not null;");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_ENCRYPTED_BYTES} blob not null;");

        transaction.ExecuteNonQuery($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_USER_ID},{KEY_TYPE})");
      }
    }

    private UserAuthenticationResource Create(DatabaseTransaction transaction, long userId, UserAuthenticationType type, byte[] payload)
    {
      int iterations = Main.Server.Config.DefaultUserAuthenticationResourceIterationCount;

      byte[] salt = RNG.GetBytes(16);
      byte[] hash = new Rfc2898DeriveBytes(payload, salt, iterations, HashAlgorithmName.SHA256).GetBytes(32);
      byte[] iv = RNG.GetBytes(16);
      byte[] challengeBytes = RNG.GetBytes(32);
      byte[] encryptedBytes = Aes.Create().CreateEncryptor(hash, iv).TransformFinalBlock(challengeBytes, 0, challengeBytes.Length);

      return DbInsert(transaction, new()
      {
        { KEY_USER_ID, userId },
        { KEY_TYPE, (byte)type },
        { KEY_ITERATIONS, iterations },
        { KEY_SALT, salt },
        { KEY_IV, iv },
        { KEY_CHALLENGE_BYTES, challengeBytes },
        { KEY_ENCRYPTED_BYTES, encryptedBytes }
      });
    }

    public UserAuthenticationResource CreatePassword(DatabaseTransaction transaction, long userId, string password)
    {
      if (!ValidPasswordRegex.IsMatch(password))
      {
        throw new ArgumentException("Invalid password.", nameof(password));
      }

      return Create(transaction, userId, UserAuthenticationType.Password, Encoding.UTF8.GetBytes(password));
    }

    public (UserAuthenticationResource userAuthentication, byte[] hashCache)? GetByPassword(DatabaseTransaction transaction, long userId, string password)
    {
      if (!ValidPasswordRegex.IsMatch(password))
      {
        return null;
      }

      using SqliteDataReader reader = DbSelect(transaction, new()
      {
        { KEY_TYPE, ("=", (byte)UserAuthenticationType.Password, null) },
        { KEY_USER_ID, ("=", userId, null) }
      }, []);

      byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
      while (reader.Read())
      {
        UserAuthenticationResource userAuthentication = Memory.ResolveFromData(CreateData(reader));

        if (userAuthentication.IsMatch(passwordBytes))
        {
          return (userAuthentication, userAuthentication.GetHash(passwordBytes));
        }
      }

      return null;
    }

    public (UserAuthenticationResource userAuthentication, byte[] hashCache)? GetByPayload(DatabaseTransaction transaction, byte[] payload)
    {
      using SqliteDataReader reader = DbSelect(transaction, [], []);

      while (reader.Read())
      {
        UserAuthenticationResource userAuthentication = Memory.ResolveFromData(CreateData(reader));

        if (userAuthentication.IsMatch(payload))
        {
          return (userAuthentication, userAuthentication.GetHash(payload));
        }
      }

      return null;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserId,
    UserAuthenticationType Type,
    int Iterations,
    byte[] Salt,
    byte[] IV,
    byte[] ChallengeBytes,
    byte[] EncryptedBytes
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_USER_ID = "userId";
    [JsonPropertyName(KEY_USER_ID)]
    public long UserId = UserId;

    public const string KEY_TYPE = "type";
    [JsonPropertyName(KEY_TYPE)]
    public UserAuthenticationType Type = Type;
  }

  public long UserId => Data.UserId;
  public UserAuthenticationType Type => Data.Type;
  public int Iterations => Data.Iterations;
  public byte[] Salt => Data.Salt;
  public byte[] IV => Data.IV;
  public byte[] ChallengeBytes => Data.ChallengeBytes;
  public byte[] EncryptedBytes => Data.EncryptedBytes;

  public byte[] GetHash(byte[] payload) => new Rfc2898DeriveBytes(payload, Salt, Iterations, HashAlgorithmName.SHA256).GetBytes(32);
  public bool IsMatch(byte[] payload)
  {
    ;
    try
    {
      return Aes.Create().CreateDecryptor(GetHash(payload), IV).TransformFinalBlock(EncryptedBytes, 0, EncryptedBytes.Length).SequenceEqual(ChallengeBytes);
    }
    catch
    {
      return false;
    }
  }
}

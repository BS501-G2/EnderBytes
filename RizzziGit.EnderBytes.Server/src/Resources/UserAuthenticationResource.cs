using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Buffer;
using Database;

public sealed class UserAuthenticationResource(UserAuthenticationResource.ResourceManager manager, UserAuthenticationResource.ResourceData data) : Resource<UserAuthenticationResource.ResourceManager, UserAuthenticationResource.ResourceData, UserAuthenticationResource>(manager, data)
{
  public const string NAME = "UserAuthentication";
  public const int VERSION = 1;

  private const string KEY_USER_ID = "UserID";
  private const string KEY_TYPE = "Type";
  private const string KEY_PAYLOAD = "Payload";

  private const string INDEX_USER_ID = $"Index_{NAME}_{KEY_USER_ID}";
  private const string INDEX_TYPE = $"Index_{NAME}_{KEY_TYPE}";

  public const string JSON_KEY_USER_ID = "userId";
  public const string JSON_KEY_TYPE = "type";

  public const byte TYPE_PASSWORD_HASH_IV = 0;

  public new sealed class ResourceData(
    ulong id,
    long createTime,
    long updateTime,
    ulong userId,
    byte type,
    byte[] payload
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong UserID = userId;
    public byte Type = type;
    public byte[] Payload = payload;

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_USER_ID, UserID },
        { JSON_KEY_TYPE, Type }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceManager
  {
    private static readonly Regex ValidPasswordRegex = new("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[\\W_])[a-zA-Z0-9\\W_]{8,64}$");

    public ResourceManager(MainResourceManager main) : base(main, VERSION, NAME)
    {
      Generator = RandomNumberGenerator.Create();

      main.Users.ResourceDeleteListeners.Add(DeleteAllFromUser);
    }

    public readonly RandomNumberGenerator Generator;

    protected override UserAuthenticationResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (ulong)(long)reader[KEY_USER_ID],
      (byte)(long)reader[KEY_TYPE],
      (byte[])reader[KEY_PAYLOAD]
    );

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {Name} add column {KEY_USER_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {Name} add column {KEY_TYPE} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {Name} add column {KEY_PAYLOAD} blob not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_USER_ID} on {NAME}({KEY_USER_ID})", cancellationToken);
        await connection.ExecuteNonQueryAsync($"create index {INDEX_TYPE} on {NAME}({KEY_TYPE})", cancellationToken);
      }
    }

    public Task<ResourceStream> Stream(SQLiteConnection connection, UserResource user, (int? offset, int length)? limit, CancellationToken cancellationToken) => DbSelect(connection, new()
    {
      { KEY_USER_ID, ("=", user.ID) }
    }, limit, null, cancellationToken);

    public Task<bool> DeleteAllFromUser(SQLiteConnection connection, UserResource user, CancellationToken cancellationToken) => DbDelete(connection, new() {
      { KEY_USER_ID, ("=", user.ID) }
    }, cancellationToken);

    private static byte[] GeneratePasswordHash(string password, int iterations, byte[] salt) => new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256).GetBytes(32);
    private static byte[] GenerateChallengeFromHash(byte[] hash, byte[] iv, byte[] raw) => Aes.Create().CreateEncryptor(hash, iv).TransformFinalBlock(raw, 0, raw.Length);
    private static bool ComapreChallengeFromHash(byte[] hash, byte[] iv, byte[] raw, byte[] encrypted) => Aes.Create().CreateDecryptor(hash, iv).TransformFinalBlock(encrypted, 0, encrypted.Length).SequenceEqual(raw);

    public static bool ComparePasswordHash(in byte[] payload, string password)
    {
      try
      {
        return ComapreChallengeFromHash(GeneratePasswordHash(password, BitConverter.ToInt32(payload.AsSpan()[32..36]), payload[16..32]), payload[0..16], payload[36..52], payload[52..84]);
      }
      catch (CryptographicException)
      {
        return false;
      }
    }

    public async Task<UserAuthenticationResource> CreatePassword(SQLiteConnection connection, UserResource user, string? oldPassword, string password, CancellationToken cancellationToken)
    {
      if (!ValidPasswordRegex.IsMatch(password))
      {
        throw new ArgumentException("Password is invalid.", nameof(password));
      }

      (UserAuthenticationResource oldAuthentication, byte[] oldHash)? old = null;
      List<UserAuthenticationResource> toDelete = await (await DbSelect(connection, new()
      {
        { KEY_USER_ID, ("=", user.ID) },
        { KEY_TYPE, ("=", TYPE_PASSWORD_HASH_IV) }
      }, null, null, cancellationToken)).ToList(cancellationToken);

      if (toDelete.Count != 0)
      {
        if (oldPassword == null)
        {
          throw new ArgumentException("Old existing password is required.", nameof(oldPassword));
        }

        UserAuthenticationResource oldAuthentication = toDelete.Last();
        if (!ComparePasswordHash(oldAuthentication.Payload, password))
        {
          throw new ArgumentException("Invalid old password.", nameof(oldPassword));
        }

        old = (oldAuthentication, GeneratePasswordHash(password, BitConverter.ToInt32(oldAuthentication.Payload.AsSpan()[32..36]), oldAuthentication.Payload[16..32]));
      }

      byte[] payload = new byte[84];
      int iterations = Main.Server.Config.DefaultPasswordIterations;
      Array.Copy(BitConverter.GetBytes(iterations), 0, payload, 32, 4);
      Generator.GetBytes(payload, 16, 16);
      Generator.GetBytes(payload, 36, 16);
      Generator.GetBytes(payload, 0, 16);

      byte[] newHash = GeneratePasswordHash(password, iterations, payload[16..32]);
      Array.Copy(GenerateChallengeFromHash(newHash, payload[0..16], payload[36..52]), 0, payload, 52, 32);

      UserAuthenticationResource newAuthentication = await DbInsert(connection, new()
      {
        { KEY_USER_ID, user.ID },
        { KEY_TYPE, TYPE_PASSWORD_HASH_IV },
        { KEY_PAYLOAD, payload }
      }, cancellationToken);

      if (old != null)
      {
        var (oldAuthentication, oldHash) = old.Value;

        await Main.BlobStorageFileKeys.Clone(connection, oldAuthentication, oldHash, newAuthentication, newHash, cancellationToken);
        foreach (UserAuthenticationResource userAuthentication in toDelete)
        {
          await Delete(connection, userAuthentication, cancellationToken);
        }
      }

      return newAuthentication;
    }

    public async Task<bool> ComparePassword(SQLiteConnection connection, UserResource user, string password, CancellationToken cancellationToken)
    {
      await using var stream = await Stream(connection, user, null, cancellationToken);
      await foreach (UserAuthenticationResource authentication in stream)
      {
        switch (authentication.Type)
        {
          case TYPE_PASSWORD_HASH_IV:
            if (!ComparePasswordHash(authentication.Payload, password))
            {
              continue;
            }

            return true;
        }
      }

      return false;
    }

    // public async Task<UserAuthenticationResource> CreatePasswordHashIV(SQLiteConnection connection, UserResource user, string? oldPassword, string newPassword, CancellationToken cancellationToken)
    // {
    //   if (!ValidPasswordRegex.IsMatch(newPassword))
    //   {
    //     throw new ArgumentException("Password is invalid.", nameof(newPassword));
    //   }
    //   (UserAuthenticationResource oldAuthentication, byte[] oldHash)? old = null;

    //   List<UserAuthenticationResource> toDelete = await (await DbSelect(connection, new()
    //   {
    //     { KEY_USER_ID, ("=", user.ID) },
    //     { KEY_TYPE, ("=", TYPE_PASSWORD_HASH_IV) }
    //   }, null, null, cancellationToken)).ToList(cancellationToken);

    //   if (toDelete.Count != 0)
    //   {
    //     if (oldPassword == null)
    //     {
    //       throw new ArgumentException("Old password is required.", nameof(oldPassword));
    //     }

    //     UserAuthenticationResource oldAuthentication = toDelete.Last();
    //     byte[] oldPayload = oldAuthentication.Payload;
    //     byte[] oldSalt = oldPayload[16..32];
    //     int oldIterations = BitConverter.ToInt32(oldPayload[32..36]);
    //     byte[] oldHash = new byte[32];
    //     Array.Copy(GeneratePasswordHash(oldPassword, oldIterations, oldSalt), 0, oldHash, 0, 20);

    //     old = (oldAuthentication, oldHash);
    //   }


    //   byte[] salt = new byte[16];
    //   int iterations = Main.Server.Config.DefaultPasswordIterations;

    //   byte[] payload = GeneratePasswordHashIV(newPassword, iterations, salt);
    //   byte[] hash = new byte[32];
    //   Array.Copy(GeneratePasswordHash(newPassword, iterations, salt), 0, hash, 0, 20);
    //   UserAuthenticationResource newAuthentication = await DbInsert(connection, new()
    //   {
    //     { KEY_USER_ID, user.ID },
    //     { KEY_TYPE, TYPE_PASSWORD_HASH_IV },
    //     { KEY_PAYLOAD, payload }
    //   }, cancellationToken);

    //   if (old != null)
    //   {
    //     (UserAuthenticationResource oldAuthentication, byte[] oldHash) = old.Value;
    //     await Main.BlobStorageFileKeys.Clone(connection, oldAuthentication, oldHash, newAuthentication, hash, cancellationToken);
    //   }

    //   return newAuthentication;
    // }
  }

  public ulong UserID => Data.UserID;
  public byte Type => Data.Type;
  public byte[] Payload => Data.Payload;
}

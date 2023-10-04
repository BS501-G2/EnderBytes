using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

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

  public const int TYPE_PASSWORD = 0;

  public const int EXCEPTION_CREATE_RESOURCE_PASSWORD_INVALID = 1 << 0;

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    ulong userId,
    byte type,
    byte[] payload
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong UserID = userId;
    public byte Type = type;
    public byte[] Payload = payload;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      UserID = data.UserID;
      Type = data.Type;
      Payload = data.Payload;
    }

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

      main.Users.ResourceDeleteHandlers.Add(DeleteAllFromUser);
    }

    public readonly RandomNumberGenerator Generator;

    protected override UserAuthenticationResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
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

    private byte[] GeneratePasswordHash(string password) => GeneratePasswordHash(password, Main.Server.Config.DefaultPasswordIterations);
    private byte[] GeneratePasswordHash(string password, int iterations) => GeneratePasswordHash(password, iterations, null);
    private byte[] GeneratePasswordHash(string password, int iterations, Span<byte> customSalt) => GeneratePasswordHash(password, iterations, customSalt.ToArray());
    private byte[] GeneratePasswordHash(string password, int iterations, byte[]? customSalt)
    {
      byte[] output = new byte[40];

      Span<byte> salt = new(output, 0, 16);
      Span<byte> hash = new(output, 16, 20);

      if (customSalt != null)
      {
        customSalt.CopyTo(salt);
      }
      else
      {
        Generator.GetBytes(salt);
      }

      Rfc2898DeriveBytes rfc2898DeriveBytes = new(password, salt.ToArray(), iterations, HashAlgorithmName.SHA256);
      rfc2898DeriveBytes.GetBytes(20).CopyTo(hash);
      Array.Copy(BitConverter.GetBytes(iterations), 0, output, 36, 4);

      return output;
    }

    public async Task<UserAuthenticationResource> CreatePassword(SQLiteConnection connection, UserResource user, string password, CancellationToken cancellationToken)
    {
      if (!ValidPasswordRegex.IsMatch(password))
      {
        throw new ArgumentException("Password is invalid.", nameof(password));
      }

      return await DbInsert(connection, new()
      {
        { KEY_USER_ID, user.ID },
        { KEY_TYPE, TYPE_PASSWORD },
        { KEY_PAYLOAD, GeneratePasswordHash(password) }
      }, cancellationToken);
    }

    public async Task<bool> ComparePasswordHash(SQLiteConnection connection, UserResource user, string rawPassword, CancellationToken cancellationToken)
    {
      await using var stream = await Stream(connection, user, null, cancellationToken);
      await foreach (UserAuthenticationResource authentication in stream)
      {
        if (
          (authentication.Type != TYPE_PASSWORD) ||
          (!ComparePasswordHash(rawPassword, authentication.Payload))
        )
        {
          continue;
        }

        return true;
      }

      return false;
    }

    public bool ComparePasswordHash(string rawPassword, byte[] encryptedPassword) => GeneratePasswordHash(rawPassword, BitConverter.ToInt32(encryptedPassword, 36), new Span<byte>(encryptedPassword, 0, 16)).SequenceEqual(encryptedPassword);
  }

  public ulong UserID => Data.UserID;
  public byte Type => Data.Type;
  public byte[] Payload => Data.Payload;
}

using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using System.Security.Cryptography;
using Database;

public partial class UserAuthenticationResource
{
  public const string NAME = "UserAuthentication";
  public const int VERSION = 1;

  public const string KEY_USER_ID = "UserID";
  public const string KEY_TYPE = "Type";
  public const string KEY_PAYLOAD = "Payload";

  public const string INDEX_USER_ID = $"Index_{NAME}_{KEY_USER_ID}";
  public const string INDEX_TYPE = $"Index_{NAME}_{KEY_TYPE}";

  public new class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceManager(main, VERSION, NAME)
  {
    public RandomNumberGenerator Generator = RandomNumberGenerator.Create();

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

    public bool ComparePasswordHash(string rawPassword, byte[] encryptedPassword) => GeneratePasswordHash(rawPassword, BitConverter.ToInt32(encryptedPassword, 36), new Span<byte>(encryptedPassword, 0, 16)).SequenceEqual(encryptedPassword);
  }
}

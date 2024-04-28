using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using DatabaseWrappers;
using Services;
using Newtonsoft.Json;

public sealed record UserPair(UserManager.Resource User, UserAuthenticationToken AuthenticationToken);

public sealed partial class UserManager(ResourceService service) : ResourceManager<UserManager, UserManager.Resource>(service, NAME, VERSION)
{
  public sealed class UsernameNotFoundException(string Username) : Exception($"Username not found: {Username}.");

  public new sealed partial record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,
    string Username,
    string LastName,
    string FirstName,
    string? MiddleName,
    byte[] PublicKey
  ) : ResourceManager<UserManager, Resource>.Resource(Id, CreateTime, UpdateTime)
  {
    [JsonIgnore]
    public byte[] PublicKey = PublicKey;

    private RSACryptoServiceProvider? RSACryptoServiceProvider;

    ~Resource() => RSACryptoServiceProvider?.Dispose();

    public byte[] Encrypt(KeyService service, byte[] bytes)
    {
      lock (this)
      {
        RSACryptoServiceProvider ??= service.GetRsaCryptoServiceProvider(PublicKey);

        return RSACryptoServiceProvider.Encrypt(bytes, false);
      }
    }
  }

  public const string NAME = "User";
  public const int VERSION = 1;

  public const string COLUMN_USERNAME = "Username";
  public const string COLUMN_PUBLIC_KEY = "PublicKey";

  public const string COLUMN_LAST_NAME = "LastName";
  public const string COLUMN_FIRST_NAME = "FirstName";
  public const string COLUMN_MIDDLE_NAME = "MiddleName";

  public const string INDEX_USERNAME = $"Index_{NAME}_{COLUMN_USERNAME}";

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetString(reader.GetOrdinal(COLUMN_USERNAME)),
    reader.GetString(reader.GetOrdinal(COLUMN_LAST_NAME)),
    reader.GetString(reader.GetOrdinal(COLUMN_FIRST_NAME)),
    reader.GetStringOptional(reader.GetOrdinal(COLUMN_MIDDLE_NAME)),
    reader.GetBytes(reader.GetOrdinal(COLUMN_PUBLIC_KEY))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_USERNAME} varchar(16) not null collate {DatabaseWrapper switch
      {
        MySQLDatabase => "utf8_general_ci",
        _ => "nocase"
      }};");
      await SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_PUBLIC_KEY} blob null;");
      await SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_LAST_NAME} varchar(32) not null;");
      await SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_FIRST_NAME} varchar(32) not null;");
      await SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_MIDDLE_NAME} varchar(32) null;");

      await SqlNonQuery(transaction, $"create unique index {INDEX_USERNAME} on {Name}({COLUMN_USERNAME});");
    }
  }

  public async Task<UserPair> Create(ResourceService.Transaction transaction, string username, string lastName, string firstName, string? middleName, string password)
  {
    (byte[] privateKey, byte[] publicKey) = Service.Server.KeyService.GetNewRsaKeyPair();

    Resource user = await InsertAndGet(transaction, new(
      (COLUMN_USERNAME, await FilterValidUsername(transaction, username)),
      (COLUMN_PUBLIC_KEY, publicKey),
      (COLUMN_LAST_NAME, lastName),
      (COLUMN_FIRST_NAME, firstName),
      (COLUMN_MIDDLE_NAME, middleName)
    ));

    return new(user, await Service.GetManager<UserAuthenticationManager>().CreatePassword(transaction, user, password, privateKey, publicKey));
  }

  public async Task<bool> Update(ResourceService.Transaction transaction, Resource user, string username, string lastName, string firstName, string? middleName)
  {
    return await Update(transaction, user, new(
      (COLUMN_USERNAME, FilterValidUsername(transaction, username)),
      (COLUMN_LAST_NAME, lastName),
      (COLUMN_FIRST_NAME, firstName),
      (COLUMN_MIDDLE_NAME, middleName)
    ));
  }

  public async Task<Resource?> GetByUsername(ResourceService.Transaction transaction, string username)
  {
    if (ValidateUsername(username) != UsernameValidationFlag.NoErrors)
    {
      return null;
    }

    return await SelectFirst(transaction, new WhereClause.CompareColumn(COLUMN_USERNAME, "=", username));
  }

  public async Task<long> CountUsers(ResourceService.Transaction transaction) => (long)(await SqlScalar(transaction, $"select count(*) from {NAME};"))!;
}

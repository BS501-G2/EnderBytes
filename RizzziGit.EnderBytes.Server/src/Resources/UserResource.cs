using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using DatabaseWrappers;
using Services;
using Newtonsoft.Json;

public sealed partial class UserResource(UserResource.ResourceManager manager, UserResource.ResourceData data) : Resource<UserResource.ResourceManager, UserResource.ResourceData, UserResource>(manager, data)
{
  public sealed record UserPair(UserResource User, UserAuthenticationResource.UserAuthenticationToken AuthenticationToken);

  public const string NAME = "User";
  public const int VERSION = 1;

  public new sealed partial class ResourceManager(ResourceService service) : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager(service, NAME, VERSION)
  {
    public const string COLUMN_USERNAME = "Username";
    public const string COLUMN_PUBLIC_KEY = "PublicKey";

    public const string COLUMN_LAST_NAME = "LastName";
    public const string COLUMN_FIRST_NAME = "FirstName";
    public const string COLUMN_MIDDLE_NAME = "MiddleName";

    public const string INDEX_USERNAME = $"Index_{NAME}_{COLUMN_USERNAME}";

    protected override UserResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetString(reader.GetOrdinal(COLUMN_USERNAME)),
      reader.GetString(reader.GetOrdinal(COLUMN_LAST_NAME)),
      reader.GetString(reader.GetOrdinal(COLUMN_FIRST_NAME)),
      reader.GetStringOptional(reader.GetOrdinal(COLUMN_MIDDLE_NAME)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_PUBLIC_KEY))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_USERNAME} varchar(16) not null collate {DatabaseWrapper switch
        {
          MySQLDatabase => "utf8_general_ci",
          _ => "nocase"
        }};");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_PUBLIC_KEY} blob null;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_LAST_NAME} varchar(32) not null;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_FIRST_NAME} varchar(32) not null;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_MIDDLE_NAME} varchar(32) null;");

        SqlNonQuery(transaction, $"create unique index {INDEX_USERNAME} on {Name}({COLUMN_USERNAME});");
      }
    }

    public UserPair Create(ResourceService.Transaction transaction, string username, string lastName, string firstName, string? middleName, string password, CancellationToken cancellationToken = default)
    {
      (byte[] privateKey, byte[] publicKey) = Service.Server.KeyService.GetNewRsaKeyPair();

      UserResource user = InsertAndGet(transaction, new(
        (COLUMN_USERNAME, FilterValidUsername(transaction, username)),
        (COLUMN_PUBLIC_KEY, publicKey),
        (COLUMN_LAST_NAME, lastName),
        (COLUMN_FIRST_NAME, firstName),
        (COLUMN_MIDDLE_NAME, middleName)
      ), cancellationToken);

      return new(user, Service.GetManager<UserAuthenticationResource.ResourceManager>().CreatePassword(transaction, user, password, privateKey, publicKey));
    }

    public bool Update(ResourceService.Transaction transaction, UserResource user, string username, string lastName, string firstName, string? middleName)
    {
      lock (user)
      {
        user.ThrowIfInvalid();

        return Update(transaction, user, new(
          (COLUMN_USERNAME, FilterValidUsername(transaction, username)),
          (COLUMN_LAST_NAME, lastName),
          (COLUMN_FIRST_NAME, firstName),
          (COLUMN_MIDDLE_NAME, middleName)
        ));
      }
    }

    public bool TryGetByUsername(ResourceService.Transaction transaction, string username, [NotNullWhen(true)] out UserResource? user, CancellationToken cancellationToken = default)
    {
      if (ValidateUsername(username) != UsernameValidationFlag.NoErrors)
      {
        user = null;
        return false;
      }

      return (user = Select(transaction, new WhereClause.CompareColumn(COLUMN_USERNAME, "=", username), new(1), cancellationToken: cancellationToken).FirstOrDefault()) != null;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    string Username,
    string LastName,
    string FirstName,
    string? MiddleName,
    byte[] PublicKey) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(Id, CreateTime, UpdateTime);

  public string Username => Data.Username;
  public string LastName => Data.LastName;
  public string FirstName => Data.FirstName;
  public string? MiddleName => Data.MiddleName;
  [JsonIgnore]
  public byte[] PublicKey => Data.PublicKey;

  private RSACryptoServiceProvider? RSACryptoServiceProvider;

  ~UserResource() => RSACryptoServiceProvider?.Dispose();

  public byte[] Encrypt(byte[] bytes)
  {
    lock (this)
    {
      RSACryptoServiceProvider ??= Manager.Service.Server.KeyService.GetRsaCryptoServiceProvider(PublicKey);

      return RSACryptoServiceProvider.Encrypt(bytes, false);
    }
  }
}

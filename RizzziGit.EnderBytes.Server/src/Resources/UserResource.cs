using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using DatabaseWrappers;
using Services;

public sealed partial class UserResource(UserResource.ResourceManager manager, UserResource.ResourceData data) : Resource<UserResource.ResourceManager, UserResource.ResourceData, UserResource>(manager, data)
{
  public sealed record UserPair(UserResource User, UserAuthenticationResource.UserAuthenticationToken AuthenticationToken);

  public const string NAME = "User";
  public const int VERSION = 1;

  public new sealed partial class ResourceManager(ResourceService service) : Resource<ResourceManager, ResourceData, UserResource>.ResourceManager(service, NAME, VERSION)
  {
    public const string COLUMN_USERNAME = "Username";
    public const string COLUMN_DISPLAY_NAME = "DisplayName";
    public const string COLUMN_PUBLIC_KEY = "PublicKey";

    public const string INDEX_USERNAME = $"Index_{NAME}_{COLUMN_USERNAME}";

    protected override UserResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetString(reader.GetOrdinal(COLUMN_USERNAME)),
      reader.GetStringOptional(reader.GetOrdinal(COLUMN_DISPLAY_NAME)),
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
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_DISPLAY_NAME} varchar(32) null;");
        SqlNonQuery(transaction, $"alter table {Name} add column {COLUMN_PUBLIC_KEY} blob null;");

        SqlNonQuery(transaction, $"create unique index {INDEX_USERNAME} on {Name}({COLUMN_USERNAME});");
      }
    }

    public UserPair Create(ResourceService.Transaction transaction, string username, string? displayName, string password, CancellationToken cancellationToken = default)
    {
      (byte[] privateKey, byte[] publicKey) = Service.Server.KeyService.GetNewRsaKeyPair();

      UserResource user = InsertAndGet(transaction, new(
        (COLUMN_USERNAME, FilterValidUsername(transaction, username)),
        (COLUMN_DISPLAY_NAME, displayName),
        (COLUMN_PUBLIC_KEY, publicKey)
      ), cancellationToken);

      return new(user, Service.GetResourceManager<UserAuthenticationResource.ResourceManager>().CreatePassword(transaction, user, password, privateKey, publicKey));
    }

    public bool Update(ResourceService.Transaction transaction, UserResource user, string username, string? displayName)
    {
      lock (user)
      {
        user.ThrowIfInvalid();

        return Update(transaction, user, new(
          (COLUMN_USERNAME, FilterValidUsername(transaction, username)),
          (COLUMN_DISPLAY_NAME, displayName)
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

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, string Username, string? DisplayName, byte[] PublicKey) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(Id, CreateTime, UpdateTime);

  public string Username => Data.Username;
  public string? DisplayName => Data.DisplayName;
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

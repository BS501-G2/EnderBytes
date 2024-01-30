using System.Data.SQLite;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Resources;

using System.Security.Cryptography;
using Services;
using Utilities;

public sealed class FileHubResource(FileHubResource.ResourceManager manager, FileHubResource.ResourceData data) : Resource<FileHubResource.ResourceManager, FileHubResource.ResourceData, FileHubResource>(manager, data)
{
  [Flags]
  public enum FileHubFlags : byte
  {
    Personal = 1 << 0
  }

  private const string NAME = "FileHub";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileHubResource>.ResourceManager
  {
    private const string COLUMN_OWNER_USER_ID = "UserId";
    private const string COLUMN_NAME = "Name";
    private const string COLUMN_FLAGS = "Flags";
    private const string COLUMN_AES_KEY = "AesKey";
    private const string COLUMN_AES_IV = "AesIv";
    private const string COLUMN_DELETION_SCHEDULE = "DeletionSchedule";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
      service.Users.ResourceDeleted += (transaction, resource) =>
      {
        Delete(transaction, new WhereClause.Nested("and",
          new WhereClause.Raw($"({COLUMN_FLAGS} & {{0}}) != 0", (byte)FileHubFlags.Personal),
          new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", resource.Id)
        ));
      };
    }

    protected override FileHubResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_OWNER_USER_ID)),
      (FileHubFlags)reader.GetByte(reader.GetOrdinal(COLUMN_FLAGS)),
      reader.GetString(reader.GetOrdinal(COLUMN_NAME)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_AES_KEY)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_AES_IV)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_DELETION_SCHEDULE))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_OWNER_USER_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FLAGS} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_DELETION_SCHEDULE} integer null;");
      }
    }

    public FileHubResource GetPersonal(ResourceService.Transaction transaction, UserResource user, UserAuthenticationResource userAuthentication)
    {
      return SelectOne(transaction, new WhereClause.Nested("and",
        new WhereClause.Raw($"({COLUMN_FLAGS} & {{0}}) != 0", (byte)FileHubFlags.Personal),
        new WhereClause.CompareColumn(COLUMN_OWNER_USER_ID, "=", user.Id)
      )) ?? insertNew();

      FileHubResource insertNew()
      {
        byte[] aesKey = RandomNumberGenerator.GetBytes(32);
        byte[] aesIv = RandomNumberGenerator.GetBytes(16);

        return Insert(transaction, new(
          (COLUMN_OWNER_USER_ID, user.Id),
          (COLUMN_FLAGS, (byte)FileHubFlags.Personal),
          (COLUMN_NAME, $"Bucket of User #{user.Id}"),
          (COLUMN_AES_KEY, userAuthentication.Encrypt(aesKey)),
          (COLUMN_AES_IV, aesIv),
          (COLUMN_DELETION_SCHEDULE, null)
        ));
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long OwnerUserId,
    FileHubFlags Flags,
    string Name,
    byte[] RsaKey,
    byte[] RsaIv,
    long? DeletionSchedule
  ) : Resource<ResourceManager, ResourceData, FileHubResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long OwnerUserId => Data.OwnerUserId;
  public FileHubFlags Flags => Data.Flags;
  public string Name => Data.Name;
  public byte[] RsaKey => Data.RsaKey;
  public byte[] RsaIv => Data.RsaIv;
  public long? DeletionSchedule => Data.DeletionSchedule;
}

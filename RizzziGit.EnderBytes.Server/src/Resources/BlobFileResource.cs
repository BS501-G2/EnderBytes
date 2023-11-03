using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using System.Security.Cryptography;
using Database;
using RizzziGit.EnderBytes.Extensions;

public sealed class BlobFileResource : Resource<BlobFileResource.ResourceManager, BlobFileResource.ResourceData, BlobFileResource>
{
  public const byte TYPE_FILE = 0;
  public const byte TYPE_FOLDER = 1;
  public const byte TYPE_SYMBOLIC_LINK = 2;

  public BlobFileResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceManager
  {
    private const string NAME = "BlobFile";
    private const int VERSION = 1;

    private const string KEY_ACCESS_TIME = "AccessTime";
    private const string KEY_USER_ID = "UserId";
    private const string KEY_IV = "Iv";
    private const string KEY_TYPE = "Type";
    private const string KEY_NAME = "Name";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      RNG = RandomNumberGenerator.Create();
    }

    private readonly RandomNumberGenerator RNG;

    protected override BlobFileResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_ACCESS_TIME],
      (long)reader[KEY_USER_ID],
      (byte[])reader[KEY_IV],
      (byte)reader[KEY_TYPE],
      (string)reader[KEY_NAME]
    );

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_ACCESS_TIME} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_USER_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_IV} blob not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_TYPE} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_NAME} varchar(128) not null;");
      }
    }

    public (BlobFileResource file, BlobFileKeyResource fileKey) Create(
      DatabaseTransaction transaction,
      UserResource user,
      UserAuthenticationResource userAuthentication,
      KeyResource key,
      byte type,
      BlobFileResource? parentFolder,
      string name
    )
    {
      if (parentFolder != null && parentFolder.Type != TYPE_FOLDER)
      {
        throw new ArgumentException("Invalid file resource type.", nameof(parentFolder));
      };

      BlobFileResource file = DbInsert(transaction, new()
      {
        { KEY_ACCESS_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
        { KEY_USER_ID, user.Id },
        { KEY_IV, RNG.GetBytes(16) },
        { KEY_TYPE, type },
        { KEY_NAME, name }
      });

      return (file, Main.BlobFileKeys.Create(transaction, file, key, userAuthentication));
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long AccessTime,
    long UserId,
    byte[] Iv,
    byte Type,
    string Name
  ) : Resource<ResourceManager, ResourceData, BlobFileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long AccessTime => Data.AccessTime;
  public long UserId => Data.UserId;
  public byte[] Iv => Data.Iv;
  public byte Type => Data.Type;
  public string Name => Data.Name;
}

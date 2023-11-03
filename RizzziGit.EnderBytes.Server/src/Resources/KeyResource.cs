using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    private const string NAME = "Name";
    private const int VERSION = 1;

    private const string KEY_OWNER_USER_ID = "Owner";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override KeyResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_OWNER_USER_ID} integer not null;");
      }
    }

    public (KeyResource key, KeyDataResource keyData) Create(DatabaseTransaction transaction, UserAuthenticationResource userAuthentication, byte[] hashCache)
    {
      KeyResource key = DbInsert(transaction, new()
      {
        { KEY_OWNER_USER_ID, userAuthentication.UserId }
      });

      return (key, Main.KeyData.Create(transaction, key, userAuthentication, hashCache));
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime);
}

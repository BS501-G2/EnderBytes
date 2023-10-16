using Microsoft.Data.Sqlite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

public sealed class KeyResource(KeyResource.ResourceManager manager, KeyResource.ResourceData data) : Resource<KeyResource.ResourceManager, KeyResource.ResourceData, KeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, KeyResource>.ResourceManager
  {
    public const string NAME = "Key";
    public const int VERSION = 1;

    private const string KEY_AUTH_ID = "UserId";
    private const string KEY_INDEX = "KeyIndex";
    private const string KEY_IV = "IV";
    private const string KEY_PAYLOAD = "Payload";

    public ResourceManager(MainResourceManager main, Database.Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override KeyResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {

    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, KeyResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
  }
}

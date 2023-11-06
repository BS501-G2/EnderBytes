using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class TableVersionResource(TableVersionResource.ResourceManager manager, TableVersionResource.ResourceData data) : Resource<TableVersionResource.ResourceManager, TableVersionResource.ResourceData, TableVersionResource>(manager, data)
{
  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, string Name, int Version) : Resource<ResourceManager, ResourceData, TableVersionResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
    public const string KEY_NAME = "name";
    [JsonPropertyName(KEY_NAME)]
    public readonly string Name = Name;

    public const string KEY_VERSION = "version";
    [JsonPropertyName(KEY_VERSION)]
    public readonly int Version = Version;
  }

  public new sealed class ResourceManager(MainResourceManager main, Database database) : Resource<ResourceManager, ResourceData, TableVersionResource>.ResourceManager(main, database, NAME, VERSION)
  {
    public const string NAME = "TableVersion";
    public const int VERSION = 1;

    private const string KEY_NAME = "Name";
    private const string KEY_VERSION = "Version";

    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (string)reader[KEY_NAME],
      (int)(long)reader[KEY_VERSION]
    );

    protected override TableVersionResource CreateResource(ResourceData data) => new(this, data);

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0) => throw new NotImplementedException("Method not implemented.");

    public bool SetVersion(DatabaseTransaction transaction, string name, int version)
    {
      if (transaction.ExecuteNonQuery($"update {NAME} set {KEY_VERSION} = {{0}} where {KEY_NAME} = {{1}};", version, name) == 0)
      {
        transaction.ExecuteNonQuery($"insert into {NAME}({KEY_NAME},{KEY_VERSION}) values ({{0}},{{1}});", name, version);
      }

      return true;
    }

    public int? GetVersion(DatabaseTransaction transaction, string name)
    {
      return (int?)(long?)transaction.ExecuteScalar($"select {KEY_VERSION} from {NAME} where {KEY_NAME} = {{0}} limit 1;", name);
    }

    public new void Init(DatabaseTransaction transaction)
    {
      transaction.ExecuteNonQuery($"create table if not exists {NAME}({KEY_NAME} varchar(128) primary key,{KEY_VERSION} integer not null);");
    }
  }

  public string Name => Data.Name;
  public int Version => Data.Version;
}

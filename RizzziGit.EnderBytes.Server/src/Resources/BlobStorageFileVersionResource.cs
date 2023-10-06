using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Newtonsoft.Json.Linq;

public sealed class BlobStorageFileVersionResource(BlobStorageFileVersionResource.ResourceManager manager, BlobStorageFileVersionResource.ResourceData data) : Resource<BlobStorageFileVersionResource.ResourceManager, BlobStorageFileVersionResource.ResourceData, BlobStorageFileVersionResource>(manager, data)
{
  public const string NAME = "VSFVersion";
  public const int VERSION = 1;

  private const string KEY_FILE_ID = "FileID";
  private const string KEY_VERSION = "Version";
  private const string KEY_SIZE = "Size";

  private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_FILE_ID}_{KEY_VERSION}";

  public const string JSON_KEY_FILE_ID = "fileId";
  public const string JSON_KEY_VERSION = "version";
  public const string JSON_KEY_SIZE = "size";

  public new sealed class ResourceData(
    ulong id,
    long createTime,
    long updateTime,
    ulong fileId,
    int version,
    long size
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileVersionResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong FileID = fileId;
    public int Version = version;
    public long Size = size;

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_FILE_ID, FileID },
        { JSON_KEY_VERSION, Version },
        { JSON_KEY_SIZE, Size }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, BlobStorageFileVersionResource>.ResourceManager(main, VERSION, NAME)
  {
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime,
      (ulong)(long)reader[KEY_FILE_ID],
      (int)(long)reader[KEY_VERSION],
      (long)reader[KEY_SIZE]
    );

    protected override BlobStorageFileVersionResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_FILE_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_VERSION} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_SIZE} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create unique index {INDEX_UNIQUENESS} on {NAME}({KEY_FILE_ID},{KEY_VERSION});", cancellationToken);
      }
    }
  }
}

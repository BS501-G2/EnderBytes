using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobDataMapResource(BlobDataMapResource.ResourceManager manager, BlobDataMapResource.ResourceData data) : Resource<BlobDataMapResource.ResourceManager, BlobDataMapResource.ResourceData, BlobDataMapResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobDataMapResource>.ResourceManager
  {
    private const string NAME = "BlobMap";
    private const int VERSION = 1;

    private const string KEY_VERSION_ID = "VersionId";
    private const string KEY_DATA_ID = "DataId";

    public ResourceManager(BlobStorageResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      main.Versions.ResourceDeleted += (transaction, resource) => DbDelete(transaction, new()
      {
        { KEY_VERSION_ID, ("=", resource.Id) }
      });

      ResourceDeleted += (transaction, resource) =>
      {
        foreach (BlobDataMapResource _ in DbStream(transaction, new()
        {
          { KEY_DATA_ID, ("=", resource.Data.Id) }
        }, new(1, null))) return;

        BlobDataResource? data = main.Data.GetById(transaction, resource.DataId);
        if (data == null)
        {
          return;
        }

        main.Data.Delete(transaction, data);
      };
    }

    protected override BlobDataMapResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_VERSION_ID],
      (long)reader[KEY_DATA_ID]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_VERSION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_DATA_ID} integer not null;");
      }
    }

    public BlobDataMapResource Create(DatabaseTransaction transaction, BlobFileVersionResource version, BlobDataResource data) => DbInsert(transaction, new()
    {
      { KEY_VERSION_ID, version.Id },
      { KEY_DATA_ID, data.Id }
    });

    public void Clone(DatabaseTransaction transaction, BlobFileVersionResource toVersion, BlobFileVersionResource fromVersion)
    {
      foreach (BlobDataMapResource entry in DbStream(transaction, new()
      {
        { KEY_VERSION_ID, ("=", fromVersion.Id) }
      }))
      {
        DbInsert(transaction, new()
        {
          { KEY_VERSION_ID, toVersion.Id },
          { KEY_DATA_ID, entry.DataId }
        });
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long VersionId,
    long DataId
  ) : Resource<ResourceManager, ResourceData, BlobDataMapResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long VersionId => Data.VersionId;
  public long DataId => Data.DataId;
}

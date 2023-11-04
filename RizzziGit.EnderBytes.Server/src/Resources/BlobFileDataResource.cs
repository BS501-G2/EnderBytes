using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

public sealed class BlobFileDataResource : Resource<BlobFileDataResource.ResourceManager, BlobFileDataResource.ResourceData, BlobFileDataResource>
{
  public BlobFileDataResource(ResourceManager manager, ResourceData data) : base(manager, data)
  {
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileDataResource>.ResourceManager
  {
    private const string NAME = "BlobFileData";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_VERSION_ID = "VersionId";
    private const string KEY_BLOB_OFFSET = "BlobOffset";
    private const string KEY_BLOB_LENGTH = "BlobLength";

    public ResourceManager(MainResourceManager main, Database.Database database) : base(main, database, NAME, VERSION)
    {
      Main.BlobFileVersions.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_VERSION_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override BlobFileDataResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_VERSION_ID],
      (long)reader[KEY_BLOB_OFFSET],
      (long)reader[KEY_BLOB_LENGTH]
    );

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_VERSION_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BLOB_OFFSET} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BLOB_LENGTH} integer not null;");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long FileVersionId,
    long BlobOffset,
    long BlobLength
  ) : Resource<ResourceManager, ResourceData, BlobFileDataResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long FileVersionId => Data.FileVersionId;
  public long BlobOffset => Data.BlobOffset;
  public long BlobLength => Data.BlobLength;
}

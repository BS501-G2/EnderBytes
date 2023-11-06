using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobFileDataResource(BlobFileDataResource.ResourceManager manager, BlobFileDataResource.ResourceData data) : Resource<BlobFileDataResource.ResourceManager, BlobFileDataResource.ResourceData, BlobFileDataResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileDataResource>.ResourceManager
  {
    private const string NAME = "BlobFileDataResource";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_SNAPSHOT_ID = "SnapshotId";
    private const string KEY_BLOB_ADDRESS = "Address";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      Main.Files.OnResourceDelete((transaction, resource, cancellationToken) => DbUpdate(transaction, new()
      {
        { KEY_FILE_ID, null },
        { KEY_SNAPSHOT_ID, null }
      }, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      }, cancellationToken));

      Main.FileSnapshots.OnResourceDelete((transaction, resource, cancellationToken) => DbUpdate(transaction, new()
      {
        { KEY_FILE_ID, null },
        { KEY_SNAPSHOT_ID, null }
      }, new()
      {
        { KEY_SNAPSHOT_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    protected override BlobFileDataResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader[KEY_FILE_ID] is DBNull ? null : (long)reader[KEY_FILE_ID],
      reader[KEY_SNAPSHOT_ID] is DBNull ? null : (long)reader[KEY_SNAPSHOT_ID],
      (long)reader[KEY_BLOB_ADDRESS]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_SNAPSHOT_ID} integer;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BLOB_ADDRESS} integer not null;");
      }
    }

    private long GetAvailableAddress(DatabaseTransaction transaction)
    {
      foreach (BlobFileDataResource available in Stream(transaction.ExecuteReader(
        $"select * from {NAME} as t1 where {KEY_FILE_ID} is null and not exists (select 1 from {NAME} as t2 where t1.{KEY_BLOB_ADDRESS} = t2.{KEY_BLOB_ADDRESS} and t2.{KEY_FILE_ID} and t2.{KEY_FILE_ID} is not null) limit 1;"
      )))
      {
        return available.BlobAddress;
      }

      foreach (BlobFileDataResource available in DbStream(transaction, [], (1, null), [(KEY_BLOB_ADDRESS, "desc")]))
      {
        return available.BlobAddress + 1;
      }

      return 0;
    }

    public async Task<BlobFileDataResource> Create(DatabaseTransaction transaction, BlobFileResource file, BlobFileSnapshotResource? snapshot, long? available, CancellationToken cancellationToken)
    {
      available ??= GetAvailableAddress(transaction);

      if (await DbUpdate(transaction, new()
        {
          { KEY_FILE_ID, file.Id },
          { KEY_SNAPSHOT_ID, snapshot?.Id }
        }, new()
        {
          { KEY_BLOB_ADDRESS, ("=", available, null) }
        }, cancellationToken) != 0
      )
      {
        foreach (BlobFileDataResource data in DbStream(transaction, new()
        {
          { KEY_FILE_ID, ("=", file.Id, null) },
          { KEY_SNAPSHOT_ID, ("=", snapshot?.Id, null) }
        }, (1, null)))
        {
          return data;
        }

        throw new InvalidOperationException("Failed to retrieve updated data.");
      }

      return DbInsert(transaction, new()
      {
        { KEY_SNAPSHOT_ID, file.Id },
        { KEY_SNAPSHOT_ID, snapshot?.Id },
        { KEY_BLOB_ADDRESS, GetAvailableAddress(transaction) }
      });
    }

    public async Task<List<BlobFileDataResource>> CopyFrom(DatabaseTransaction transaction, BlobFileResource file, BlobFileSnapshotResource toSnapshot, BlobFileSnapshotResource? fromSnapshot, CancellationToken cancellationToken)
    {
      List<BlobFileDataResource> list = [];
      UserResource user = Main.Users.GetById(transaction, file.Id) ?? throw new InvalidOperationException("User does not exist.");

      foreach (BlobFileDataResource data in DbStream(transaction, new()
      {
        { KEY_FILE_ID, ("=", file.Id, null) },
        { KEY_SNAPSHOT_ID, ("=", fromSnapshot?.Id, null) }
      }))
      {
        list.Add(await Create(transaction, file, toSnapshot, null, cancellationToken));
      }

      return list;
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long? FileId,
    long? SnapshotId,
    long BlobAddress
  ) : Resource<ResourceManager, ResourceData, BlobFileDataResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? FileId => Data.FileId;
  public long? SnapshotId => Data.SnapshotId;
  public long BlobAddress => Data.BlobAddress;
}

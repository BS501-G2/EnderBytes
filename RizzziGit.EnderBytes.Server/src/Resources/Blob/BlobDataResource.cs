using Microsoft.Data.Sqlite;
namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Database;

public sealed class BlobDataResource(BlobDataResource.ResourceManager manager, BlobDataResource.ResourceData data) : Resource<BlobDataResource.ResourceManager, BlobDataResource.ResourceData, BlobDataResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobDataResource>.ResourceManager
  {
    private const int BUFFER_SIZE = 1024 * 256;

    private const string NAME = "BlobData";
    private const int VERSION = 1;

    private const string KEY_BYTES = "Bytes";

    public ResourceManager(BlobStorageResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
    }

    protected override BlobDataResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (byte[])reader[KEY_BYTES]
    );

    protected override void OnInit(DatabaseTransaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_BYTES} blob not null;");
      }
    }

    public BlobDataResource Create(DatabaseTransaction transaction, byte[] data)
    {
      if (data.Length != BUFFER_SIZE)
      {
        throw new ArgumentException("Buffer size invalid.");
      }

      return DbInsert(transaction, new()
      {
        { KEY_BYTES, data }
      });
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    byte[] Bytes
  ) : Resource<ResourceManager, ResourceData, BlobDataResource>.ResourceData(Id, CreateTime, UpdateTime);

  public byte[] Bytes => Data.Bytes;
}

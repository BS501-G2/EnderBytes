using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

public sealed class BlobStorageFileBlobResource(BlobStorageFileBlobResource.ResourceManager manager, BlobStorageFileBlobResource.ResourceData data) : Resource<BlobStorageFileBlobResource.ResourceManager, BlobStorageFileBlobResource.ResourceData, BlobStorageFileBlobResource>(manager, data)
{
  public const string NAME = "VSBlob";
  public const int VERSION = 1;

  public new sealed class ResourceData(
    ulong id,
    long createTime,
    long updateTime
  ) : Resource<ResourceManager, ResourceData, BlobStorageFileBlobResource>.ResourceData(id, createTime, updateTime)
  {
  }

  public new sealed class ResourceManager(MainResourceManager main) : Resource<ResourceManager, ResourceData, BlobStorageFileBlobResource>.ResourceManager(main, VERSION, NAME)
  {
    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override BlobStorageFileBlobResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    protected override Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}

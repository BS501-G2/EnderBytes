using System.Data.SQLite;
using RizzziGit.EnderBytes.Services;

namespace RizzziGit.EnderBytes.Resources;

public sealed class FileDataResource(FileDataResource.ResourceManager manager, FileDataResource.ResourceData data) : Resource<FileDataResource.ResourceManager, FileDataResource.ResourceData, FileDataResource>(manager, data)
{
  private const string NAME = "FileData";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService service) : Resource<ResourceManager, ResourceData, FileDataResource>.ResourceManager(service, ResourceService.Scope.Files, NAME, VERSION)
  {
    private const string COLUMN_FILE_ID = "FileId";
    private const string COLUMN_FILE_SNAPSHOT_ID = "FileSnapshotId";
    private const string COLUMN_BLOB_ADDRESS = "BlobAddress";

    protected override FileDataResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : Resource<ResourceManager, ResourceData, FileDataResource>.ResourceData(Id, CreateTime, UpdateTime);
}

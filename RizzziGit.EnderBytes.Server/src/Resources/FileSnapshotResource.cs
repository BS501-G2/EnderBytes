using System.Data.SQLite;
using RizzziGit.EnderBytes.Services;

namespace RizzziGit.EnderBytes.Resources;

public sealed class FileSnapshotResource(FileSnapshotResource.ResourceManager manager, FileSnapshotResource.ResourceData data) : Resource<FileSnapshotResource.ResourceManager, FileSnapshotResource.ResourceData, FileSnapshotResource>(manager, data)
{
  private const string NAME = "FileSnapshot";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceManager
  {
    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
    }

    protected override FileSnapshotResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, FileSnapshotResource>.ResourceData(Id, CreateTime, UpdateTime);
}

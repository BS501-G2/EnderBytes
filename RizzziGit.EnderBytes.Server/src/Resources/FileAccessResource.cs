using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class FileAccessResource(FileAccessResource.ResourceManager manager, FileAccessResource.ResourceData data) : Resource<FileAccessResource.ResourceManager, FileAccessResource.ResourceData, FileAccessResource>(manager, data)
{
  private const string NAME = "FileAccess";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService service, ResourceService.Scope scope, string name, int version) : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceManager(service, scope, name, version)
  {
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime)
    {
      throw new NotImplementedException();
    }

    protected override FileAccessResource NewResource(ResourceData data)
    {
      throw new NotImplementedException();
    }

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceData(Id, CreateTime, UpdateTime);
}

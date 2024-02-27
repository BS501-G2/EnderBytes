using System.Data.Common;
using RizzziGit.EnderBytes.Services;

namespace RizzziGit.EnderBytes.Resources;

public sealed partial class FileLinkResource(FileLinkResource.ResourceManager manager, FileLinkResource.ResourceData data) : Resource<FileLinkResource.ResourceManager, FileLinkResource.ResourceData, FileLinkResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceManager
  {
    public ResourceManager(ResourceService service, string name, int version) : base(service, name, version)
    {
      
    }

    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime)
    {
      throw new NotImplementedException();
    }

    protected override FileLinkResource NewResource(ResourceData data)
    {
      throw new NotImplementedException();
    }

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceData(Id, CreateTime, UpdateTime)
  {
  }
}

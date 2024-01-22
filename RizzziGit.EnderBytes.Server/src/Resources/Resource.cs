using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Services;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public abstract partial class ResourceManager(ResourceService service, ResourceService.Scope scope, string name, int version) : ResourceService.ResourceManager(service, scope, name, version)
  {
    public delegate void ResourceDeleteHandler(R resource);
    public delegate void ResourceUpdateHandler(R resource, D oldData);
    public delegate void ResourceInsertHandler(R resource);

    private readonly Dictionary<long, R> Resources = [];

    protected abstract R NewResource(D data);
    protected abstract D CastToData(SqliteDataReader reader, long id, long createTime, long updateTime);
    protected R GetResource(D data)
    {
      if (!Resources.TryGetValue(data.Id, out R? resource))
      {
        Resources.Add(data.Id, resource = NewResource(data));
      }
      else
      {
        resource.Data = data;
      }

      return resource;
    }
  }

  public abstract partial record ResourceData(long Id, long CreateTime, long UpdateTime);

  public readonly M Manager = manager;
  protected D Data { get; private set; } = data;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;
}

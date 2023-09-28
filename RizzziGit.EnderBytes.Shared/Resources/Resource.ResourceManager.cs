namespace RizzziGit.EnderBytes.Shared.Resources;

using Collections;

public abstract partial class Resource<M, D, R> : IResource
{
  public abstract partial class ResourceManager(MainResourceManager main, int version, string name) : MainResourceManager.ResourceManager(main), IResourceManager
  {
    public readonly int Version = version;
    public readonly string Name = name;

    private readonly WeakDictionary<ulong, R> Resources = new();

    protected R AsResource (D data)
    {
      lock (Resources)
      {
        if (Resources.TryGetValue(data.ID, out R? value))
        {
          value.UpdateData(data);

          return value;
        }
        else
        {
          R resource = CreateResource(data);

          Resources.Add(data.ID, resource);
          return resource;
        }
      }
    }

    protected R? GetFromMemoryByID(ulong id)
    {
      if (Resources.TryGetValue(id, out R? value))
      {
        return value;
      }

      return null;
    }

    public abstract R CreateResource(D data);
    public abstract Task Init(CancellationToken cancellationToken);
  }
}

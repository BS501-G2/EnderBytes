namespace RizzziGit.EnderBytes.Shared.Resources;

using System.Text.Json.Serialization;
using Collections;

public abstract class Resource<M, D, R>
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public abstract class ResourceManager
  {
    public sealed class ResourceMemory(ResourceManager manager) : WeakDictionary<long, R>
    {
      public readonly ResourceManager Manager = manager;

      public R ResolveFromData(D data)
      {
        if (TryGetValue(data.Id, out var value))
        {
          return value;
        }

        R resource = Manager.CreateResource(data);
        TryAdd(data.Id, resource);
        return resource;
      }
    }

    protected ResourceManager(MainResourceManager main)
    {
      Main = main;
      Memory = new(this);
    }

    public readonly MainResourceManager Main;

    protected ResourceMemory Memory;

    protected abstract R CreateResource(D data);
  }

  public abstract record ResourceData(long Id)
  {
    public const string KEY_ID = "id";

    [JsonPropertyName(KEY_ID)]
    public long Id = Id;
  }

  protected Resource(ResourceManager manager, D data)
  {
    Manager = manager;
    Data = data;
  }

  public readonly ResourceManager Manager;
  protected D Data;

  public long Id => Data.Id;
}

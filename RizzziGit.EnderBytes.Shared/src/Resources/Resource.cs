namespace RizzziGit.EnderBytes.Shared.Resources;

using Collections;
using Newtonsoft.Json.Linq;

public interface IResource;
public interface IResourceData;
public interface IResourceManager
{
  public Task Init(CancellationToken cancellationToken);
}

public abstract partial class Resource<M, D, R>(M manager, D data) : IResource
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public const string JSON_KEY_ID = "id";
  public const string JSON_KEY_CREATE_TIME = "createTime";
  public const string JSON_KEY_UPDATE_TIME = "updateTime";

  public abstract class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime
  ) : IResourceData
  {
    public ulong ID = id;
    public ulong CreateTime = createTime;
    public ulong UpdateTime = updateTime;

    public virtual void CopyFrom(D data)
    {
      if (this == data)
      {
        return;
      }

      if (ID != data.ID)
      {
        throw new InvalidOperationException("Inconsistent data ID.");
      }

      CreateTime = data.CreateTime;
      UpdateTime = data.UpdateTime;
    }

    public virtual JObject ToJSON() => new()
    {
      { JSON_KEY_ID, ID },
      { JSON_KEY_CREATE_TIME, CreateTime },
      { JSON_KEY_UPDATE_TIME, UpdateTime }
    };
  }

  public abstract partial class ResourceManager(MainResourceManager main, int version, string name) : MainResourceManager.ResourceManager(main), IResourceManager
  {
    public abstract class ResourceEnumerator<Co, E>(Co collection, CancellationToken cancellationToken) : IAsyncEnumerator<R>
      where Co : ResourceStream<Co, E>
      where E : ResourceEnumerator<Co, E>
    {
      protected readonly Co Stream = collection;
      protected readonly CancellationToken CancellationToken = cancellationToken;

      public abstract R Current { get; }
      public abstract ValueTask DisposeAsync();
      public abstract ValueTask<bool> MoveNextAsync();
    }

    public abstract class ResourceStream<Co, E>(M manager) : IAsyncEnumerable<R>, IAsyncDisposable
      where Co : ResourceStream<Co, E>
      where E : ResourceEnumerator<Co, E>
    {
      public readonly M Manager = manager;

      public abstract Task<bool> MoveNext(CancellationToken cancellationToken);
      public abstract R GetCurrent();

      public abstract ValueTask DisposeAsync();

      protected abstract ResourceEnumerator<Co, E> GetAsyncEnumerator(CancellationToken cancellationToken = default);
      IAsyncEnumerator<R> IAsyncEnumerable<R>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
    }

    public readonly int Version = version;
    public readonly string Name = name;

    private readonly WeakDictionary<ulong, R> Resources = new();

    protected R AsResource(D data)
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

    protected abstract R CreateResource(D data);
    public abstract Task Init(CancellationToken cancellationToken);
  }

  public class Exception(int code, string? message = null, Exception? innerException = null) : System.Exception(message, innerException)
  {
    public readonly int Code = code;
  }

  public class ResourceManagerException(int code, string? message = null, Exception? innerException = null) : Exception(code, message, innerException);
  public class CreateResourceException(int code, string? message = null, Exception? innerException = null) : ResourceManagerException(code, message, innerException);

  protected readonly M Manager = manager;
  protected readonly D Data = data;

  public ulong ID => Data.ID;
  public ulong CreateTime => Data.CreateTime;
  public ulong UpdateTime => Data.UpdateTime;

  public JObject ToJSON() => Data.ToJSON();
  protected virtual void UpdateData(D data) => Data.CopyFrom(data);
}

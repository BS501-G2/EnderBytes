namespace RizzziGit.EnderBytes.Web;

using Resources;

public sealed class WebApiContext
{
  public static implicit operator WebApiContext(WebApi instance) => instance.ApiContext;

  public class InstanceHolder<T>
  {
    public static implicit operator T(InstanceHolder<T> instance) => instance.Required();

    public InstanceHolder(WebApiContext context, string name, T? value = default)
    {
      Context = context;
      Name = name;

      if (value is not null)
      {
        context.SetInstance(name, value);
      }
    }

    private readonly string Name;
    private readonly WebApiContext Context;

    public T Set(T value) => Context.SetInstance(Name, value);

    public T Required() => Context.GetInstanceRequired<T>(Name);
    public T? Optional() => Context.GetInstanceOptional<T>(Name);
  }

  public UserAuthenticationToken? Token = null;

  private readonly Dictionary<string, object?> ContextInstances = [];

  private static string InstanceKey<T>(string prefixKey)
  {
    return $"{prefixKey}_{typeof(T).FullName}";
  }

  public T SetInstance<T>(string name, T instance)
  {
    ContextInstances.Add(InstanceKey<T>(name), instance);
    return instance;
  }

  public T GetInstanceRequired<T>(string name) => (T)ContextInstances[InstanceKey<T>(name)]!;
  public T? GetInstanceOptional<T>(string name)
  {
    if (!ContextInstances.TryGetValue(InstanceKey<T>(name), out object? value))
    {
      return default;
    }

    return (T)value!;
  }
}

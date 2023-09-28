namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class MainResourceManager
{
  public abstract class ResourceManager
  {
    private static ulong GenerateAndRegisterID(IResourceManager manager, MainResourceManager main)
    {
      ulong id;

      do
      {
        id = (ulong)Random.Shared.NextInt64();
      }
      while(main.ResourceManagers.TryAdd(id, new(manager)));

      return id;
    }

    protected ResourceManager(MainResourceManager main)
    {
      Main = main;

      if (this is IResourceManager manager)
      {
        ID = GenerateAndRegisterID(manager, main);
      }
      else
      {
        throw new InvalidOperationException($"Must implement {nameof(IResourceManager)} interface.");
      }
    }

    ~ResourceManager() => Main.ResourceManagers.Remove(ID);

    public readonly MainResourceManager Main;

    private readonly ulong ID;
  }

  private readonly Dictionary<ulong, WeakReference<IResourceManager>> ResourceManagers = [];
}

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    public sealed partial class DatabaseWrapper(ResourceManager manager)
    {
      public readonly ResourceManager Manager = manager;
    }
  }
}

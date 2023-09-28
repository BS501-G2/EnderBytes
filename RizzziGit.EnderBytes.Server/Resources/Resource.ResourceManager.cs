namespace RizzziGit.EnderBytes.Resources;

using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    protected ResourceManager(MainResourceManager main, int version, string name) : base(main, version, name)
    {
      Main = main;
    }

    public new readonly MainResourceManager Main;

    public Database Database => Main.RequireDatabase();
  }
}

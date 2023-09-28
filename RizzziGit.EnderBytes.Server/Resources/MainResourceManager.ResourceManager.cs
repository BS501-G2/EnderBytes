namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed partial class MainResourceManager
{
  public new abstract partial class ResourceManager(MainResourceManager main) : Shared.Resources.MainResourceManager.ResourceManager(main)
  {
    public new readonly MainResourceManager Main = main;
    public readonly EnderBytesServer Server = main.Server;
    public Database Database => Main.RequireDatabase();
  }
}

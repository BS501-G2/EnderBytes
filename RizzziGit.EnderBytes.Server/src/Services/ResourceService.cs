namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;
using DatabaseWrappers;

public sealed partial class ResourceService : Server.SubService
{
  public ResourceService(Server server) : base(server, "Resources")
  {
    ResourceManagers = [];

    ResourceManagers.Add(new UserResource.ResourceManager(this));
    ResourceManagers.Add(new UserAuthenticationResource.ResourceManager(this));
    ResourceManagers.Add(new UserConfigurationResource.ResourceManager(this));
    ResourceManagers.Add(new StorageResource.ResourceManager(this));
    ResourceManagers.Add(new FileResource.ResourceManager(this));
    ResourceManagers.Add(new FileAccessResource.ResourceManager(this));
    ResourceManagers.Add(new FileSnapshotResource.ResourceManager(this));
    ResourceManagers.Add(new FileBufferResource.ResourceManager(this));
    ResourceManagers.Add(new FileBufferMapResource.ResourceManager(this));
  }

  private Database? Database;
  private readonly List<ResourceManager> ResourceManagers;

  public T GetResourceManager<T>() where T : ResourceManager
  {
    foreach (ResourceManager resourceManager in ResourceManagers)
    {
      if (resourceManager is T t)
      {
        return t;
      }
    }

    throw new ArgumentException("Specified type is not available.");
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    Database = new MySQLDatabase(Server.Configuration.DatabaseConnectionStringBuilder);

    foreach (ResourceManager resourceManager in ResourceManagers)
    {
      await resourceManager.Start(cancellationToken);
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await await Task.WhenAny(
      RunPeriodicCheck(cancellationToken),
      WatchDog([.. ResourceManagers], cancellationToken)
    );
  }

  protected override async Task OnStop(Exception? exception = null)
  {
    foreach (ResourceManager resourceManager in ResourceManagers.Reverse<ResourceManager>())
    {
      await resourceManager.Stop();
    }

    Database?.Dispose();
    Database = null;
  }
}

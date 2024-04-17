namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;
using DatabaseWrappers;

public sealed partial class ResourceService : Server.SubService
{
  public abstract class Exception(string? message = null) : System.Exception(message ?? "A resource error has been thrown.");

  public ResourceService(Server server) : base(server, "Resources")
  {
    ResourceManagers = [];

    ResourceManagers.Add(new UserManager(this));
    ResourceManagers.Add(new UserAuthenticationManager(this));
    ResourceManagers.Add(new UserAuthenticationSessionTokenManager(this));
    ResourceManagers.Add(new UserConfigurationManager(this));
    ResourceManagers.Add(new StorageManager(this));
    ResourceManagers.Add(new FileManager(this));
    ResourceManagers.Add(new FileAccessManager(this));
    ResourceManagers.Add(new FileSnapshotManager(this));
    ResourceManagers.Add(new FileBufferManager(this));
    ResourceManagers.Add(new FileBufferMapManager(this));
  }

  private Database? Database;
  private readonly List<ResourceManager> ResourceManagers;

  public T GetManager<T>() where T : ResourceManager
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

  protected override async Task OnStop(System.Exception? exception = null)
  {
    foreach (ResourceManager resourceManager in ResourceManagers.Reverse<ResourceManager>())
    {
      await resourceManager.Stop();
    }

    Database?.Dispose();
    Database = null;
  }
}

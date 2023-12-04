namespace RizzziGit.EnderBytes.Resources;

using Database;

public interface IMainResourceManager
{
  public Server Server { get; }
  public Database Database { get; }
  public TableVersionResource.ResourceManager TableVersion { get; }
  public Logger Logger { get; }
}

public sealed class ResourceManager : Service, IMainResourceManager
{
  public ResourceManager(Server server) : base("Resources", server)
  {
    Server = server;

    Database = new(this, server.Configuration.DatabasePath, "Main");

    TableVersion = new(this, Database);
    Users = new(this, Database);
    UserKeys = new(this, Database);
    UserRoles = new(this, Database);
    UserAuthentications = new(this, Database);
    StoragePools = new(this, Database);
  }

  public Server Server { get; private set; }
  public Database Database { get; private set; }
  public TableVersionResource.ResourceManager TableVersion { get; private set; }
  public new Logger Logger => base.Logger;
  public readonly UserResource.ResourceManager Users;
  public readonly UserRoleResource.ResourceManager UserRoles;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserKeyResource.ResourceManager UserKeys;
  public readonly StoragePoolResource.ResourceManager StoragePools;
  public readonly MountPointResource.ResourceManager MountPoints;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Database.Start();
    await Database.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
      Users.Init(transaction);
      UserKeys.Init(transaction);
      UserRoles.Init(transaction);
      UserAuthentications.Init(transaction);
      StoragePools.Init(transaction);
    }, cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await (await WatchDog([Database], cancellationToken)).task;
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Database.Stop();
  }
}

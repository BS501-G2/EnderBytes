namespace RizzziGit.EnderBytes.Resources;

using Database;

public interface IMainResourceManager
{
  public Server Server { get; }
  public Database Database { get; }
  internal TableVersionResource.ResourceManager TableVersion { get; }
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

  public readonly Server Server;
  public readonly Database Database;
  internal readonly TableVersionResource.ResourceManager TableVersion;

  TableVersionResource.ResourceManager IMainResourceManager.TableVersion => TableVersion;
  Server IMainResourceManager.Server => Server;
  Database IMainResourceManager.Database => Database;
  Logger IMainResourceManager.Logger => Logger;

  public readonly UserResource.ResourceManager Users;
  public readonly UserRoleResource.ResourceManager UserRoles;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserKeyResource.ResourceManager UserKeys;
  public readonly KeyResource.ResourceManager Keys;
  public readonly StoragePoolResource.ResourceManager StoragePools;

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
      Keys.Init(transaction);
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

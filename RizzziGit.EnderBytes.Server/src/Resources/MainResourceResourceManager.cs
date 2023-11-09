namespace RizzziGit.EnderBytes.Resources;

using Database;

public interface IMainResourceManager
{
  public Server Server { get; }
  public Database MainDatabase { get; }
  public TableVersionResource.ResourceManager TableVersion { get; }
  public Logger Logger { get; }
}

public sealed class MainResourceManager : Service, IMainResourceManager
{
  public MainResourceManager(Server server) : base("Resources", server)
  {
    Server = server;

    MainDatabase = new(this, server.Configuration.DatabasePath, "Main");

    TableVersion = new(this, MainDatabase);
    Users = new(this, MainDatabase);
    UserKeys = new(this, MainDatabase);
    UserRoles = new(this, MainDatabase);
    UserAuthentications = new(this, MainDatabase);
    StoragePools = new(this, MainDatabase);
  }

  public Server Server { get; private set; }
  public Database MainDatabase { get; private set; }
  public TableVersionResource.ResourceManager TableVersion { get; private set; }
  public new Logger Logger => base.Logger;
  public readonly UserResource.ResourceManager Users;
  public readonly UserRoleResource.ResourceManager UserRoles;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserKeyResource.ResourceManager UserKeys;
  public readonly StoragePoolResource.ResourceManager StoragePools;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await MainDatabase.Start();
    await MainDatabase.RunTransaction((transaction) =>
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
    await (await WatchDog([MainDatabase], cancellationToken)).task;
  }

  protected override async Task OnStop(Exception? exception)
  {
    await MainDatabase.Stop();
  }
}

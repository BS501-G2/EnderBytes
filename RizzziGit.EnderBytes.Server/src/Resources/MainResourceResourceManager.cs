namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class MainResourceManager : Service
{
  public MainResourceManager(Server server) : base("Resource Manager")
  {
    Server = server;

    MainDatabase = new(Server, server.Config.DatabasePath, "Main");

    TableVersion = new(this, MainDatabase);
    Users = new(this, MainDatabase);
    UserAuthentications = new(this, MainDatabase);
    StoragePools = new(this, MainDatabase);
    Keys = new(this, MainDatabase);

    Server.Logger.Subscribe(Logger);
  }

  public readonly Server Server;
  public readonly Database MainDatabase;
  public readonly TableVersionResource.ResourceManager TableVersion;
  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly StoragePoolResource.ResourceManager StoragePools;
  public readonly KeyResource.ResourceManager Keys;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await MainDatabase.Start();
    await MainDatabase.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
      Users.Init(transaction);
      UserAuthentications.Init(transaction);
      StoragePools.Init(transaction);
      Keys.Init(transaction);
    }, cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await Task.Delay(-1, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await MainDatabase.Stop();
  }
}

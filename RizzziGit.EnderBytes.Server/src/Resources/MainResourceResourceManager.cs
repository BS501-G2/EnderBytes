namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class MainResourceManager : Service
{
  public MainResourceManager(Server server) : base("Resources", server)
  {
    Server = server;

    MainDatabase = new(Server, server.Configuration.DatabasePath, "Main");

    TableVersion = new(this, MainDatabase);
    Users = new(this, MainDatabase);
    UserRoles = new(this, MainDatabase);
    UserAuthentications = new(this, MainDatabase);
    StoragePools = new(this, MainDatabase);
    BlobStorageFiles = new(this, MainDatabase);
    BlobStorageFileVersions = new(this, MainDatabase);
    Keys = new(this, MainDatabase);
  }

  public readonly Server Server;
  public readonly Database MainDatabase;
  public readonly TableVersionResource.ResourceManager TableVersion;
  public readonly UserResource.ResourceManager Users;
  public readonly UserRoleResource.ResourceManager UserRoles;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly StoragePoolResource.ResourceManager StoragePools;
  public readonly BlobStorageFileResource.ResourceManager BlobStorageFiles;
  public readonly BlobStorageFileVersionResource.ResourceManager BlobStorageFileVersions;
  public readonly KeyResource.ResourceManager Keys;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await MainDatabase.Start();
    await MainDatabase.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
      Users.Init(transaction);
      UserRoles.Init(transaction);
      UserAuthentications.Init(transaction);
      StoragePools.Init(transaction);
      Keys.Init(transaction);
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

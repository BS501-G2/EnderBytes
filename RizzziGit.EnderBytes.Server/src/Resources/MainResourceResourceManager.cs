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
    BlobFiles = new(this, MainDatabase);
    BlobFileKeys = new(this, MainDatabase);
    BlobFileVersions = new(this, MainDatabase);
    Keys = new(this, MainDatabase);
    KeyData = new(this, MainDatabase);
  }

  public readonly Server Server;
  public readonly Database MainDatabase;
  public readonly TableVersionResource.ResourceManager TableVersion;
  public readonly UserResource.ResourceManager Users;
  public readonly UserRoleResource.ResourceManager UserRoles;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly StoragePoolResource.ResourceManager StoragePools;
  public readonly BlobFileResource.ResourceManager BlobFiles;
  public readonly BlobFileKeyResource.ResourceManager BlobFileKeys;
  public readonly BlobFileVersionResource.ResourceManager BlobFileVersions;
  public readonly KeyResource.ResourceManager Keys;
  public readonly KeyDataResource.ResourceManager KeyData;

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
      BlobFiles.Init(transaction);
      BlobFileKeys.Init(transaction);
      BlobFileVersions.Init(transaction);
      Keys.Init(transaction);
      KeyData.Init(transaction);
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

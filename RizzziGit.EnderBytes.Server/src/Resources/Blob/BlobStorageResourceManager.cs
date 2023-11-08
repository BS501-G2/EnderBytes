namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using StoragePools;
using Keys;
using Resources;
using Database;

public sealed class BlobStorageResourceManager : Service, IMainResourceManager
{
  public static string GetDatabaseFilePath(Server server, StoragePool storagePool) => Database.GetDatabaseFilePath(server.Configuration.BlobPath, $"{storagePool.Resource.Id}");

  public BlobStorageResourceManager(StoragePool storagePool) : base("Blob Storage Resource Manager")
  {
    if (storagePool.Resource.Type != StoragePoolType.Blob)
    {
      throw new ArgumentException("Invalid storage pool type.", nameof(storagePool));
    }

    Server = storagePool.Resource.Manager.Main.Server;
    MainDatabase = new(Server, Server.Configuration.BlobPath, $"{storagePool.Resource.Id}");
    TableVersion = new(this, MainDatabase);

    Files = new(this, MainDatabase);
    Maps = new(this, MainDatabase);
    Data = new(this, MainDatabase);
    Keys = new(this, MainDatabase);
    Versions = new(this, MainDatabase);

    storagePool.Manager.Logger.Subscribe(Logger);
  }

  public Server Server { get; private set; }
  public Database MainDatabase { get; private set; }
  public TableVersionResource.ResourceManager TableVersion { get; private set; }
  public new Logger Logger => base.Logger;

  public readonly BlobFileResource.ResourceManager Files;
  public readonly BlobDataMapResource.ResourceManager Maps;
  public readonly BlobDataResource.ResourceManager Data;
  public readonly BlobKeyResource.ResourceManager Keys;
  public readonly BlobFileVersionResource.ResourceManager Versions;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await (await WatchDog([MainDatabase], cancellationToken)).task;
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await MainDatabase.Start();
    await MainDatabase.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
      Files.Init(transaction);
      Maps.Init(transaction);
      Data.Init(transaction);
      Keys.Init(transaction);
      Versions.Init(transaction);
    }, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await MainDatabase.Stop();
  }
}

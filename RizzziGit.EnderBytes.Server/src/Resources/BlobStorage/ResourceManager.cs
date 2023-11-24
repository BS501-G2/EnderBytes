namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using StoragePools;
using Keys;
using Resources;
using Database;

public sealed class ResourceManager : Service, IMainResourceManager
{
  public static string GetDatabaseFilePath(Server server, BlobStoragePool storagePool) => Database.GetDatabaseFilePath(server.Configuration.BlobPath, $"{storagePool.Resource.Id}");

  public ResourceManager(BlobStoragePool storagePool) : base($"Blob #{storagePool.Resource.Id}")
  {
    if (storagePool.Resource.Type != StoragePoolType.Blob)
    {
      throw new ArgumentException("Invalid storage pool type.", nameof(storagePool));
    }

    StoragePool = storagePool;

    Server = storagePool.Resource.Manager.Main.Server;
    Database = new(this, Server.Configuration.BlobPath, $"{storagePool.Resource.Id}");
    TableVersion = new(this, Database);

    Nodes = new(this, Database);

    storagePool.Manager.Logger.Subscribe(Logger);
  }

  public BlobStoragePool StoragePool;

  public Server Server { get; private set; }
  public Database Database { get; private set; }
  public TableVersionResource.ResourceManager TableVersion { get; private set; }
  public new Logger Logger => base.Logger;

  public readonly FileNodeResource.ResourceManager Nodes;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await (await WatchDog([Database], cancellationToken)).task;
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Database.Start();
    await Database.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
    }, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Database.Stop();
  }
}

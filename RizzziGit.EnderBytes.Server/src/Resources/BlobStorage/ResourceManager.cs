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
    MainDatabase = new(this, Server.Configuration.BlobPath, $"{storagePool.Resource.Id}");
    TableVersion = new(this, MainDatabase);

    storagePool.Manager.Logger.Subscribe(Logger);
  }

  public BlobStoragePool StoragePool;

  public Server Server { get; private set; }
  public Database MainDatabase { get; private set; }
  public TableVersionResource.ResourceManager TableVersion { get; private set; }
  public new Logger Logger => base.Logger;

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
    }, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await MainDatabase.Stop();
  }
}

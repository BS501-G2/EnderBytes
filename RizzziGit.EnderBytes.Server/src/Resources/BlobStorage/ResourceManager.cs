namespace RizzziGit.EnderBytes.Resources.BlobStorage;

using Resources;
using Database;
using StoragePools;

public class ResourceManager : Service, IMainResourceManager
{
  public ResourceManager(BlobStoragePool storagePool, string password)
  {
    if (storagePool.Resource.Type != StoragePoolType.Blob)
    {
      throw new ArgumentException("Invalid storage pool type.", nameof(storagePool));
    }

    StoragePool = storagePool;

    Server = storagePool.Resource.Manager.Main.Server;
    Database = new(this, Server.Configuration.BlobPath, $"{storagePool.Resource.Id}", password);

    TableVersion = new(this, Database);
    Nodes = new(this, Database);
    Keys = new(this, Database);

    storagePool.Manager.Logger.Subscribe();
  }

  public readonly BlobStoragePool StoragePool;
  public readonly Server Server;
  public readonly Database Database;

  internal readonly TableVersionResource.ResourceManager TableVersion;
  public readonly BlobNodeResource.ResourceManager Nodes;
  public readonly KeyResource.ResourceManager Keys;

  Server IMainResourceManager.Server => Server;
  Database IMainResourceManager.Database => Database;
  TableVersionResource.ResourceManager IMainResourceManager.TableVersion => TableVersion;
  Logger IMainResourceManager.Logger => Logger;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Database.Start();
    await Database.RunTransaction((transaction) =>
    {
      TableVersion.Init(transaction);
      Keys.Init(transaction);
      Nodes.Init(transaction);
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

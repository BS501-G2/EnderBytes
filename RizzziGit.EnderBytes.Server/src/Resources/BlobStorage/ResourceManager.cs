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

    storagePool.Manager.Logger.Subscribe();
  }

  public readonly BlobStoragePool StoragePool;
  public readonly Server Server;
  public readonly Database Database;

  internal readonly TableVersionResource.ResourceManager TableVersion;

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
    }, cancellationToken);
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return Task.Delay(-1, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Database.Stop();
  }
}

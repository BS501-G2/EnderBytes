namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;
using Framework.Collections;

using Core;

using StorageResource = Resources.Storage;

public sealed partial class StorageService(Server server) : Server.SubService(server, "Storage")
{
  private readonly WeakDictionary<StorageResource, Storage> StorageCollection = [];

  public Storage GetStorage(StorageResource resource)
  {
    resource.ThrowIfInvalid();

    lock (this)
    {
      if (!StorageCollection.TryGetValue(resource, out Storage? storage))
      {
        storage = new(this, resource);
      }

      return storage;
    }
  }
}

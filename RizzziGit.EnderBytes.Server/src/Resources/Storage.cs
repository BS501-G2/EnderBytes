namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed partial class Storage(Storage.ResourceManager manager, Resource<Storage.ResourceManager, Storage.ResourceData, Storage>.ResourceRecord record) : Resource<Storage.ResourceManager, Storage.ResourceData, Storage>(manager, record)
{
  public StorageService.Storage.Session GetSession(ConnectionService.Connection connection) => Manager.Server.StorageService.GetStorage(this).NewSession(connection);
}

using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed partial class Storage
{
  private const string NAME = "Storage";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService main) : Resource<ResourceManager, ResourceData, Storage>.ResourceManager(main, main.Server.MainDatabase, NAME, VERSION)
  {
    protected override Storage CreateResourceClass(ResourceRecord record) => new(this, record);

    public Storage Create(long OwnerUserId, string name, UserAuthentication? userAuthentication, CancellationToken cancellationToken = default) => ExecuteSynchronized((cancellationToken) =>
    {
      (byte[] privateKey, byte[] publicKey) = Main.Server.KeyService.GetNewRsaKeyPair();
      ResourceData data = new(OwnerUserId, name, userAuthentication?.Encrypt(privateKey) ?? privateKey, publicKey);

      return Insert(data, cancellationToken);
    }, cancellationToken);
  }
}

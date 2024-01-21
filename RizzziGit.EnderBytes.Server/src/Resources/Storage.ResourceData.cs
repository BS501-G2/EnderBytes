namespace RizzziGit.EnderBytes.Resources;

public sealed partial class Storage
{
  public new sealed record ResourceData(long? OwnerUserId, string Name, byte[] PrivateKey, byte[] PublicKey) : Resource<ResourceManager, ResourceData, Storage>.ResourceData;
}

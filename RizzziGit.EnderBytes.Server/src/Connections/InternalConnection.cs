using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.Connections;

public sealed class InternalConnection(ConnectionManager manager, ulong id, UserAuthenticationResource userAuthenticationResource, byte[] hashCache) : Connection(manager, id)
{
  private readonly UserAuthenticationResource UserAuthenticationResource = userAuthenticationResource;
  private readonly byte[] HashCache = hashCache;
}

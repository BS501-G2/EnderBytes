namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ClientService
{
  public sealed class InternalClient(UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken) : Client(userAuthenticationToken)
  {
  }
}

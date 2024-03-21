namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ClientService
{
  public sealed class InternalClient(ClientService clientService, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken) : Client(clientService, userAuthenticationToken)
  {
  }
}

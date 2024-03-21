namespace RizzziGit.EnderBytes.Services;

using Resources;
using Commons.Services;

public sealed partial class ClientService
{
  public abstract partial class Client(ClientService service, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null)
  {
    public readonly ClientService Service = service;

    public UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken { get; private set; } = userAuthenticationToken;
  }
}

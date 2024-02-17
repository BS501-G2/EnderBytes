namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ConnectionService
{
  public abstract record Request
  {
    public sealed record Login(string Username, UserAuthenticationResource.UserAuthenticationType AuthenticationType, byte[] AuthenticationPayload) : Request;
    public sealed record LoginWithToken(UserAuthenticationResource.UserAuthenticationToken Token) : Request;
    public sealed record Logout() : Request;
  }
}

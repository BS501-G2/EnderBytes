namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ConnectionService
{
  public abstract partial class Connection(ConnectionService service, ConnectionConfiguration configuration, long id)
  {
    public readonly long Id = id;
    public readonly ConnectionService Service = service;
    public readonly ConnectionConfiguration Configuration = configuration;

    public SessionService.Session? Session => Service.Server.SessionService.GetSession(this);

    public bool IsValid => Service.IsConnectionValid(Id, this);
    public void ThrowIfInvalid() => Service.ThrowIfConnectionInvalid(Id, this);

    protected void Authenticate(UserAuthenticationResource.Token token) => Service.Server.SessionService.NewSession(this, token);
    protected void Authenticate(UserAuthenticationResource userAuthentication, byte[] payload) => Authenticate(userAuthentication.GetTokenByPayload(payload));
  }
}

namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ConnectionService
{
  public abstract partial class Connection(ConnectionService service, ConnectionConfiguration configuration, long id)
  {
    public readonly long Id = id;
    public readonly ConnectionService Service = service;
    public readonly ConnectionConfiguration Configuration = configuration;

    private SessionService.Session? SessionBackingField;
    public SessionService.Session? Session => SessionBackingField?.IsValid == true ? SessionBackingField : null;

    public bool IsValid => Service.IsConnectionValid(Id, this);
    public void ThrowIfInvalid() => Service.ThrowIfConnectionInvalid(Id, this);

    protected void Authenticate(UserAuthenticationResource.Token token) => SessionBackingField = Service.Server.SessionService.NewSession(this, token);
    protected void Authenticate(UserAuthenticationResource userAuthentication, byte[] payload) => Authenticate(userAuthentication.GetTokenByPayload(payload));
  }
}

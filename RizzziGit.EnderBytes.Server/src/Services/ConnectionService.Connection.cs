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

    public bool IsValid => Service.IsValid(Id, this);
    public void ThrowIfInvalid() => Service.ThrowIfInvalid(Id, this);

    protected void AuthenticateWithPayloadHash(UserAuthenticationResource userAuthentication, byte[] payloadHash)
    {
      userAuthentication.ThrowIfPayloadHashInvalid(payloadHash);

      SessionBackingField = Service.Server.SessionService.NewSession(this, userAuthentication, payloadHash);
    }

    public void AuthenticateWithPayload(UserAuthenticationResource userAuthentication, byte[] payload) => AuthenticateWithPayloadHash(userAuthentication, userAuthentication.GetPayloadHash(payload));
  }
}

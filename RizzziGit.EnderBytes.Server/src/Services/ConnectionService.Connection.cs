namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

using Resources;

using Session = SessionService.Session;

public sealed partial class ConnectionService
{
  public abstract partial class Connection : Lifetime
  {
    private Connection(ConnectionService service, Parameters parameters)
    {
      Service = service;
      Parameters = parameters;

      Id = Service.NextId++;
    }

    public readonly ConnectionService Service;
    public readonly long Id;

    protected readonly Parameters Parameters;
    public Session? Session => Service.Server.SessionService.GetSession(this);

    public virtual bool IsValid => Service.IsValid(this);
    public void ThrowIfInvalid()
    {
      if (!IsValid)
      {
        throw new InvalidOperationException("Connection is invalid.");
      }
    }

    protected Session CreateSessionWithPayload(UserAuthentication userAuthentication, byte[] payload) => Service.Server.SessionService.CreateSessionWithPayload(this, userAuthentication, payload);
    protected Session CreateSessionWithPayloadHash(UserAuthentication userAuthentication, byte[] payloadHash) => Service.Server.SessionService.CreateSessionWithPayloadHash(this, userAuthentication, payloadHash);

    public StorageService.Storage.Session Storage => Storage.Storage.NewSession(this);
  }

  private long NextId;
}

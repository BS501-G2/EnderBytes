namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

using Session = SessionService.Session;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Internal : Connection
    {
      public Internal(ConnectionService service, Parameters.Internal configuration) : base(service, configuration)
      {
        Parameters = configuration;
        Session = service.Server.SessionService.CreateSessionWithPayloadHash(this, configuration.UserAuthentication, configuration.PayloadHash);
      }

      private new readonly Parameters.Internal Parameters;
      private readonly Session Session;

      public override bool IsValid => base.IsValid && Session.IsValid;
    }
  }
}

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Internal : Connection
    {
      public Internal(ConnectionService service, Parameters.Internal configuration) : base(service, configuration)
      {
        Parameters = configuration;
        CreateSessionWithPayloadHash(configuration.UserAuthentication, configuration.PayloadHash);
      }

      private new readonly Parameters.Internal Parameters;

      public override bool IsValid => base.IsValid && (Session?.IsValid ?? false);
    }
  }
}

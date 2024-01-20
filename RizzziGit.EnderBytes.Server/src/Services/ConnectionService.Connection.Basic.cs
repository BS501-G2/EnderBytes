namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Basic(ConnectionService service, Parameters.Basic configuration) : Connection(service, configuration)
    {
      private new readonly Parameters.Basic Configuration = configuration;
    }
  }
}

namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Internal(ConnectionService service, Parameters.Internal configuration) : Connection(service, configuration)
    {
      private new readonly Parameters.Internal Configuration = configuration;
    }
  }
}

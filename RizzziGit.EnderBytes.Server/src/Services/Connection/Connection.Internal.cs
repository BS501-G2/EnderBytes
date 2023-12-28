namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    public sealed record Internal() : Configuration;
  }

  public abstract partial class Connection
  {
    public sealed class Internal(ConnectionService service, Configuration.Internal configuration) : Connection(service, configuration)
    {
      public new readonly Configuration.Internal Configuration = configuration;
    }
  }
}

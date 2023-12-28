namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    public sealed record Advanced() : Configuration;
  }

  public abstract partial class Connection
  {
    public sealed class Advanced(ConnectionService service, Configuration.Advanced configuration) : Connection(service, configuration)
    {
      public new readonly Configuration.Advanced Configuration = configuration;
    }
  }
}

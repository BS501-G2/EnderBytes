namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Advanced(ConnectionService service, Parameters.Advanced configuration) : Connection(service, configuration)
    {
      private new readonly Parameters.Advanced Configuration = configuration;
    }
  }
}

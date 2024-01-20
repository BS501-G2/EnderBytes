namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public sealed class Advanced(ConnectionService service, Parameters.Advanced parameters) : Connection(service, parameters)
    {
      private new readonly Parameters.Advanced Parameters = parameters;
    }
  }
}

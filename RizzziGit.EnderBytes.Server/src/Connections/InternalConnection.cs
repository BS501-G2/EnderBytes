namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed partial class InternalConnection : Connection<InternalConnection, InternalConnection.ConnectionConfiguration>
{
  public InternalConnection(ConnectionService service, ConnectionConfiguration configuration, long id) : base(service, configuration, id)
  {
  }

  public new sealed record ConnectionConfiguration() : Connection<InternalConnection, ConnectionConfiguration>.ConnectionConfiguration(new ConnectionEndPoint.Null(), new ConnectionEndPoint.Null());
}

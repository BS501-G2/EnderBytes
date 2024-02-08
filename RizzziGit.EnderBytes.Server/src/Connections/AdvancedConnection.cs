namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed class AdvancedConnection(ConnectionService service, AdvancedConnection.ConnectionConfiguration configuration, long id) : Connection<AdvancedConnection, AdvancedConnection.ConnectionConfiguration>(service, configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionEndPoint RemoteEndPoint,
    ConnectionEndPoint LocalEndPoint
  ) : Connection<AdvancedConnection, ConnectionConfiguration>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);
}

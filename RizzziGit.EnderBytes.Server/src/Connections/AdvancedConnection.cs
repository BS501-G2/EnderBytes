namespace RizzziGit.EnderBytes.Connections;

using Services;

public sealed class AdvancedConnection(ConnectionService service, AdvancedConnection.ConnectionConfiguration configuration, long id) : Connection<AdvancedConnection, AdvancedConnection.ConnectionConfiguration>(service, configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionService.EndPoint RemoteEndPoint,
    ConnectionService.EndPoint LocalEndPoint
  ) : Connection<AdvancedConnection, ConnectionConfiguration>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);
}

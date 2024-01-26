namespace RizzziGit.EnderBytes.Connections;

using Services;

public sealed class BasicConnection(ConnectionService service, BasicConnection.ConnectionConfiguration configuration, long id) : Connection<BasicConnection, BasicConnection.ConnectionConfiguration>(service,  configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionService.EndPoint RemoteEndPoint,
    ConnectionService.EndPoint LocalEndPoint
  ) : Connection<BasicConnection, ConnectionConfiguration>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);
}

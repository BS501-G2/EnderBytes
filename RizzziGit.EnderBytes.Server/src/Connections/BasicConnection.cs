namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed class BasicConnection(ConnectionService service, BasicConnection.ConnectionConfiguration configuration, long id) : Connection<BasicConnection, BasicConnection.ConnectionConfiguration>(service,  configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionEndPoint RemoteEndPoint,
    ConnectionEndPoint LocalEndPoint
  ) : Connection<BasicConnection, ConnectionConfiguration>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);
}

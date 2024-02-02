namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed class AdvancedConnection(ConnectionService service, AdvancedConnection.ConnectionConfiguration configuration, long id) : Connection<AdvancedConnection, AdvancedConnection.ConnectionConfiguration, AdvancedConnection.Request, AdvancedConnection.Response>(service, configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionEndPoint RemoteEndPoint,
    ConnectionEndPoint LocalEndPoint
  ) : Connection<AdvancedConnection, ConnectionConfiguration, Request, Response>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);

  public new sealed record Request() : Connection<AdvancedConnection, ConnectionConfiguration, Request, Response>.Request;
  public new sealed record Response(int Code, string? Message) : Connection<AdvancedConnection, ConnectionConfiguration, Request, Response>.Response(Code, Message);
}
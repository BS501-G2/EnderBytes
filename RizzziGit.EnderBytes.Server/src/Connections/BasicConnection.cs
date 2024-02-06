namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed class BasicConnection(ConnectionService service, BasicConnection.ConnectionConfiguration configuration, long id) : Connection<BasicConnection, BasicConnection.ConnectionConfiguration, BasicConnection.Request, BasicConnection.Response>(service,  configuration, id)
{
  public new sealed partial record ConnectionConfiguration(
    ConnectionEndPoint RemoteEndPoint,
    ConnectionEndPoint LocalEndPoint
  ) : Connection<BasicConnection, ConnectionConfiguration, Request, Response>.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);

  public new abstract partial record Request : Connection<BasicConnection, ConnectionConfiguration, Request, Response>.Request
  {

  }

  public new abstract partial record Response(Response.ResponseStatus Status, string? Message) : Connection<BasicConnection, ConnectionConfiguration, Request, Response>.Response(Status, Message)
  {
  }
}

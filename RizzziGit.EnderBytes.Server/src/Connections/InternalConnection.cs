namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed partial class InternalConnection(ConnectionService service, InternalConnection.ConnectionConfiguration configuration, long id) : Connection<InternalConnection, InternalConnection.ConnectionConfiguration, InternalConnection.Request, InternalConnection.Response>(service, configuration, id)
{
  public new sealed record ConnectionConfiguration() : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.ConnectionConfiguration(new ConnectionEndPoint.Null(), new ConnectionEndPoint.Null());

  public new sealed record Request() : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.Request;
  public new sealed record Response(int Code, string? Message) : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.Response(Code, Message);
}

namespace RizzziGit.EnderBytes.Connections;

using Services;
using Extras;

public sealed partial class InternalConnection : Connection<InternalConnection, InternalConnection.ConnectionConfiguration, InternalConnection.Request, InternalConnection.Response>
{
  public InternalConnection(ConnectionService service, ConnectionConfiguration configuration, long id) : base(service, configuration, id)
  {
  }

  public new sealed record ConnectionConfiguration() : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.ConnectionConfiguration(new ConnectionEndPoint.Null(), new ConnectionEndPoint.Null());

  public new abstract partial record Request : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.Request
  {

  }

  public new abstract partial record Response(Response.ResponseStatus Status, string? Message) : Connection<InternalConnection, ConnectionConfiguration, Request, Response>.Response(Status, Message)
  {
  }
}

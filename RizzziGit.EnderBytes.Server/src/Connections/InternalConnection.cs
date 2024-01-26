namespace RizzziGit.EnderBytes.Connections;

using Services;

public sealed partial class InternalConnection(ConnectionService service, InternalConnection.ConnectionConfiguration configuration, long id) : Connection<InternalConnection, InternalConnection.ConnectionConfiguration>(service, configuration, id)
{
  public new sealed record ConnectionConfiguration() : Connection<InternalConnection, ConnectionConfiguration>.ConnectionConfiguration(new ConnectionService.EndPoint.Null(), new ConnectionService.EndPoint.Null());
}

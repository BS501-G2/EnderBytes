namespace RizzziGit.EnderBytes.Connections;

using Services;

public abstract partial class Connection<C, CC>(ConnectionService service, CC configuration, long id) : ConnectionService.Connection(service, configuration, id)
  where C : Connection<C, CC>
  where CC : Connection<C, CC>.ConnectionConfiguration
{
  public abstract partial record ConnectionConfiguration(
    ConnectionService.EndPoint RemoteEndPoint,
    ConnectionService.EndPoint LocalEndPoint
  ) : ConnectionService.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);
}

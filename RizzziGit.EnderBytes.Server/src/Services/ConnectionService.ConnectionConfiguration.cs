namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial record ConnectionConfiguration(EndPoint RemoteEndPoint, EndPoint LocalEndPoint);
}

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Connection(ConnectionService service, ConnectionConfiguration configuration, long id)
  {
    public readonly long Id = id;
    public readonly ConnectionService Service = service;
    public readonly ConnectionConfiguration Configuration = configuration;
  }
}

namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Framework.Collections;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    private Configuration() { }
  }

  public abstract partial class Connection : Lifetime
  {
    private Connection(ConnectionService service, Configuration configuration) : base("Connection")
    {
      Service = service;
      Configuration = configuration;
      Id = Service.NextConnectionId++;
    }

    public readonly long Id;
    public readonly ConnectionService Service;
    public readonly Configuration Configuration;

    public UserService.Session? Session { get; private set; } = null;
    public WeakDictionary<long, StorageHubService.Hub.Session> HubSessions = [];

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      try
      {
        await base.OnRun(cancellationToken);
      }
      finally
      {
      }
    }
  }
}

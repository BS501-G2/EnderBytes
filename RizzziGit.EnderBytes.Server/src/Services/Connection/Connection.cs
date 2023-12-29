namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Framework.Collections;
using RizzziGit.EnderBytes.Records;
using MongoDB.Driver;
using RizzziGit.EnderBytes.Utilities;

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

    public Server Server => Service.Server;
    public UserService.Session? Session { get; private set; } = null;

    private async Task<StorageHubService.Hub.Session[]> GetOwnedStorageHubs()
    {
      List<StorageHubService.Hub.Session> sessions = [];

      long userId = Session?.UserId ?? throw new InvalidOperationException("No access.");

      List<Record.StorageHub> hubs = [];
      await RunTask(async (cancellationToken) =>
      {
        await foreach (Record.StorageHub hub in (await Server.StorageHubService.HubRecords.FindAsync((record) => record.OwnerUserId == userId, cancellationToken: cancellationToken)).ToAsyncEnumerable(cancellationToken))
        {
          hubs.Add(hub);
        }
      });
      foreach (Record.StorageHub hub in hubs)
      {
        sessions.Add(await Server.StorageHubService.Get(hub, this));
      }

      return [.. sessions];
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      await base.OnRun(cancellationToken);
    }
  }
}

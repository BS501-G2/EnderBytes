using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Core;

using Framework.Services;
using Services;

public sealed record ServerConfiguration(
  string? ServerPath,
  MongoClientSettings MongoClientSettings,
  MongoCollectionSettings? MongoCollectionSettings
);

public sealed class Server : Service
{
  public Server(ServerConfiguration configuration) : base("Server")
  {
    MongoClient = new MongoClient(configuration.MongoClientSettings);

    KeyService = new(this);
    ResourceService = new(this);

    StateChanged += (sender, state) =>
    {
      if (state != ServiceState.Stopped)
      {
        MongoClient = new MongoClient(configuration.MongoClientSettings);
      }
    };
  }

  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }

  public IMongoClient MongoClient { get; private set; }
  public IMongoDatabase MainDatabase => MongoClient.GetDatabase("EnderBytes");

  public readonly KeyService KeyService;
  public readonly ResourceService ResourceService;
  public readonly ConnectionService ConnectionService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await KeyService.Start();
    await ResourceService.Start();

    await base.OnStart(cancellationToken);
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return WatchDog([KeyService, ResourceService], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ResourceService.Stop();
    await KeyService.Stop();

    await base.OnStop(exception);
  }
}

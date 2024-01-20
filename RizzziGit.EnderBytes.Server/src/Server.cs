using MongoDB.Driver;

namespace RizzziGit.EnderBytes;

using Framework.Services;
using Services.Connection;
using Services.Key;
using Services.Session;
using Services.Subsystem;
using Services.Resource;

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
    SessionService = new(this);
    ConnectionService = new(this);
    SubsystemService = new(this);

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

  public readonly ResourceService.Main ResourceService;
  public readonly KeyService KeyService;
  public readonly Session.SessionService SessionService;
  public readonly Connection.ConnectionService ConnectionService;
  public readonly Subsystem.SubsystemService SubsystemService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await ResourceService.Start();
    await KeyService.Start();
    await ConnectionService.Start();
    await SubsystemService.Start();

    await base.OnStart(cancellationToken);
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return WatchDog([ResourceService, KeyService, SessionService, ConnectionService], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ConnectionService.Stop();
    await SessionService.Stop();
    await KeyService.Stop();
    await ResourceService.Stop();

    await base.OnStop(exception);
  }
}

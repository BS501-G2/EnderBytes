using MongoDB.Driver;

namespace RizzziGit.EnderBytes;

using Records;
using Services;
using Utilities;
using Framework.Services;

public sealed record ServerConfiguration(
  string ServerPath,
  MongoClientSettings? MongoClientSettings,
  MongoCollectionSettings? MongoCollectionSettings
);

public sealed class Server : Service
{
  public Server(ServerConfiguration configuration) : base("Server")
  {
    MongoClient = new(configuration.MongoClientSettings);
    UserService = new(this);
    KeyService = new(this);
    StorageHubService = new(this);
    ConnectionService = new(this);
  }

  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }

  public readonly MongoClient MongoClient;
  public IMongoDatabase MainDatabase => MongoClient.GetDatabase("EnderBytes");

  public readonly UserService UserService;
  public readonly KeyService KeyService;
  public readonly StorageHubService StorageHubService;
  public readonly ConnectionService ConnectionService;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await await Task.WhenAny(
      WatchDog([UserService, KeyService, ConnectionService], cancellationToken),
      Record.WatchRecordUpdates(MongoClient, MainDatabase, cancellationToken)
    );
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await UserService.Start();
    await KeyService.Start();
    await StorageHubService.Start();
    await ConnectionService.Start();
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ConnectionService.Stop();
    await StorageHubService.Stop();
    await UserService.Stop();
    await KeyService.Stop();
  }
}

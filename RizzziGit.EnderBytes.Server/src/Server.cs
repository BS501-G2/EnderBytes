using MongoDB.Driver;

namespace RizzziGit.EnderBytes;

using Records;
using Services;
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
    KeyGeneratorService = new(this);
    ConnectionService = new(this);
  }

  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }

  public readonly MongoClient MongoClient;
  public IMongoDatabase Database => MongoClient.GetDatabase("EnderBytes");
  public IMongoCollection<R> GetCollection<R>() where R : Record => Database.GetCollection<R>(nameof(R));

  public readonly UserService UserService;
  public readonly KeyGeneratorService KeyGeneratorService;
  public readonly ConnectionService ConnectionService;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await await Task.WhenAny(
      WatchDog([UserService, KeyGeneratorService, ConnectionService], cancellationToken),
      Record.RegisterOnUpdateHook(MongoClient, Database, cancellationToken)
    );
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await UserService.Start();
    await KeyGeneratorService.Start();
    await ConnectionService.Start();
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ConnectionService.Stop();
    await UserService.Stop();
    await KeyGeneratorService.Stop();
  }
}

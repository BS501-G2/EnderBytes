namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Services;

public sealed partial class ConnectionService(Server server) : Service("Connections", server)
{
  public abstract partial record Configuration
  {
    private Configuration() { }

    public sealed record Basic() : Configuration;
    public sealed record Internal(KeyService.Transformer.UserAuthentication UserAuthentication) : Configuration;
    public sealed record Advanced() : Configuration;
  }

  public abstract partial class Connection : Lifetime
  {
    public sealed class Basic(ConnectionService service, Configuration.Basic configuration) : Connection(service, configuration)
    {
      public new readonly Configuration.Basic Configuration = configuration;
    }

    public sealed class Internal(ConnectionService service, Configuration.Internal configuration) : Connection(service, configuration)
    {
      public new readonly Configuration.Internal Configuration = configuration;
    }

    public sealed class Advanced(ConnectionService service, Configuration.Advanced configuration) : Connection(service, configuration)
    {
      public new readonly Configuration.Advanced Configuration = configuration;
    }

    private Connection(ConnectionService service, Configuration configuration) : base("Connection")
    {
      Service = service;
      Configuration = configuration;
      Id = Service.NextId++;
    }

    public readonly long Id;
    public readonly ConnectionService Service;
    public readonly Configuration Configuration;

    public Server Server => Service.Server;
    public UserService.Session.ConnectionBinding? Session { get; private set; } = null;

    public StorageService.Storage.Session GetStorage(long storageId, CancellationToken cancellationToken)
    {
      lock (this)
      {
        return Server.StorageService.GetStorageSession(storageId, this, cancellationToken);
      }
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Service)
        {
          Service.Connections.Add(Id, this);
        }

        await base.OnRun(cancellationToken);
      }
      finally
      {
        lock (Service)
        {
          Service.Connections.Remove(Id);
        }
      }
    }
  }

  public readonly Server Server = server;

  private long NextId = 0;
  private readonly WeakDictionary<long, Connection> Connections = [];

  public Connection NewConnection(Configuration configuration)
  {
    lock (this)
    {
      Connection connection = configuration switch
      {
        Configuration.Advanced advancedConfiguration => new Connection.Advanced(this, advancedConfiguration),
        Configuration.Basic basicConfiguration => new Connection.Basic(this, basicConfiguration),
        Configuration.Internal internalConfiguration => new Connection.Internal(this, internalConfiguration),

        _ => throw new InvalidOperationException("Unknown configuration type.")
      };

      connection.Start(GetCancellationToken());
      return connection;
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

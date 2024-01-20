namespace RizzziGit.EnderBytes.Services.Connection;

using Framework.Collections;
using Framework.Lifetime;
using Resources;
using Services.Session;

public abstract class Connection : Lifetime
{
  public abstract class Configuration
  {
    private Configuration() { }

    public sealed class BasicConfiguration() : Configuration;
    public sealed class AdvancedConfiguration() : Configuration;
    public sealed class InternalConnection() : Configuration;
  }

  public sealed class BasicConnection(ConnectionService manager, long id) : Connection(manager, id)
  {

  }

  public sealed class AdvancedConnection(ConnectionService manager, long id) : Connection(manager, id)
  {

  }

  public sealed class InternalConnection(ConnectionService manager, long id) : Connection(manager, id)
  {

  }

  public sealed class ConnectionService(Server server) : Server.SubService(server, "Connections")
  {
    private long NextId = 0;
    private readonly WeakDictionary<long, Connection> Connections = [];

    public Connection CreateConnection()
    {
      lock (this)
      {
        long connectionId = NextId++;
        AdvancedConnection connection = new(this, connectionId);

        connection.Stopped += (_, _) => Connections.Remove(connectionId);
        Connections.Add(connection.Id, connection);
        connection.Start(GetCancellationToken());

        return connection;
      }
    }
  }

  private Connection(ConnectionService manager, long id) : base($"#{id}", manager.Logger)
  {
    Service = manager;
    Id = id;
  }

  public readonly ConnectionService Service;
  public readonly long Id;

  public Session? CurrentSession => Service.Server.SessionService.GetSession(this, default);
  public Session CreateSession(User user, UserAuthentication userAuthentication, byte[] payloadHash, CancellationToken cancellationToken = default) => Service.Server.SessionService.CreateSession(this, user, userAuthentication, payloadHash, cancellationToken);
}

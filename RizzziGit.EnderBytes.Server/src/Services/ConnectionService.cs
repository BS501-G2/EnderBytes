namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Connections;

public sealed partial class ConnectionService(Server server) : Server.SubService(server, "Connections")
{
  private long NextId = 0;
  private readonly WeakDictionary<long, Connection> Connections = [];

  public void ThrowIfInvalid(long id, Connection connection)
  {
    if (!IsValid(id, connection))
    {
      throw new InvalidOperationException("Invalid connection.");
    }
  }

  public bool IsValid(long id, Connection connection)
  {
    lock (this)
    {
      return Connections.TryGetValue(id, out Connection? testConnection) && testConnection == connection;
    }
  }

  public Connection NewConnection(ConnectionConfiguration configuration, CancellationToken cancellationToken = default)
  {
    lock (this)
    {
      cancellationToken.ThrowIfCancellationRequested();

      Connection connection = configuration switch
      {
        BasicConnection.ConnectionConfiguration basicConfiguration => new BasicConnection(this, basicConfiguration, NextId++),
        AdvancedConnection.ConnectionConfiguration advancedConfiguration => new AdvancedConnection(this, advancedConfiguration, NextId++),
        InternalConnection.ConnectionConfiguration internalConfiguration => new InternalConnection(this, internalConfiguration, NextId++),

        _ => throw new ArgumentException("Invalid configuration.", nameof(configuration))
      };

      Connections.Add(connection.Id, connection);
      return connection;
    }
  }

  protected override Task OnStop(Exception? exception = null)
  {
    lock (this)
    {
      Connections.Clear();
    }

    return base.OnStop(exception);
  }
}

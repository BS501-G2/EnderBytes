namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Connections;
using Resources;
using Extras;

public sealed partial class ConnectionService(Server server) : Server.SubService(server, "Connections")
{
  private long NextId = 0;
  private readonly WeakDictionary<long, Connection> Connections = [];

  public abstract partial record ConnectionConfiguration(ConnectionEndPoint RemoteEndPoint, ConnectionEndPoint LocalEndPoint);

  public void ThrowIfConnectionInvalid(long id, Connection connection)
  {
    if (!IsConnectionValid(id, connection))
    {
      throw new InvalidOperationException("Invalid connection.");
    }
  }

  public bool IsConnectionValid(long id, Connection connection)
  {
    lock (this)
    {
      lock (connection)
      {
        return Connections.TryGetValue(id, out Connection? testConnection) && testConnection == connection;
      }
    }
  }

  public void Close(long id, Connection connection)
  {
    lock (connection)
    {
      lock (this)
      {
        ThrowIfConnectionInvalid(id, connection);
        Connections.Remove(id);
      }
    }
  }

  public BasicConnection NewConnection(BasicConnection.ConnectionConfiguration configuration, CancellationToken cancellationToken = default) => NewConnection<BasicConnection, BasicConnection.ConnectionConfiguration>(configuration, cancellationToken);
  public AdvancedConnection NewConnection(AdvancedConnection.ConnectionConfiguration configuration, CancellationToken cancellationToken = default) => NewConnection<AdvancedConnection, AdvancedConnection.ConnectionConfiguration>(configuration, cancellationToken);
  public InternalConnection NewConnection(InternalConnection.ConnectionConfiguration configuration, CancellationToken cancellationToken = default) => NewConnection<InternalConnection, InternalConnection.ConnectionConfiguration>(configuration, cancellationToken);

  private C NewConnection<C, CC>(CC configuration, CancellationToken cancellationToken = default)
    where C : Connection<C, CC>
    where CC : Connection<C, CC>.ConnectionConfiguration
  {
    cancellationToken.ThrowIfCancellationRequested();
    lock (this)
    {
      cancellationToken.ThrowIfCancellationRequested();

      long nextId = NextId++;

      Connection connection = configuration switch
      {
        BasicConnection.ConnectionConfiguration basicConfiguration => new BasicConnection(this, basicConfiguration, NextId),
        AdvancedConnection.ConnectionConfiguration advancedConfiguration => new AdvancedConnection(this, advancedConfiguration, NextId),
        InternalConnection.ConnectionConfiguration internalConfiguration => new InternalConnection(this, internalConfiguration, NextId),

        _ => throw new ArgumentException("Invalid configuration.", nameof(configuration))
      };

      Connections.Add(connection.Id, connection);
      return (C)connection;
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

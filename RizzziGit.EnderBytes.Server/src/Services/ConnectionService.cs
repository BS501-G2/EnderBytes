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
    lock (connection)
    {
      lock (this)
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

  public C NewConnection<C, CC, Rq, Rs>(CC configuration, CancellationToken cancellationToken = default)
    where C : Connection<C, CC, Rq, Rs>
    where CC : Connection<C, CC, Rq, Rs>.ConnectionConfiguration
    where Rq : Connection<C, CC, Rq, Rs>.Request
    where Rs : Connection<C, CC, Rq, Rs>.Response
  {
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

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;

public sealed partial class ConnectionService(Server server) : Server.SubService(server, "Connections")
{
  private readonly WeakDictionary<long, Connection> Connections = [];

  public bool IsValid(Connection connection)
  {
    lock (this)
    {
      return Connections.TryGetValue(connection.Id, out Connection? existingConnection) && existingConnection == connection;
    }
  }

  public Connection NewConnection(Parameters configuration, CancellationToken cancellationToken = default) => ExecuteSynchronized((cancellationToken) =>
  {
    Connection connection = configuration switch
    {
      Parameters.Basic internalConfiguration => new Connection.Basic(this, internalConfiguration),
      Parameters.Advanced internalConfiguration => new Connection.Advanced(this, internalConfiguration),
      Parameters.Internal internalConfiguration => new Connection.Internal(this, internalConfiguration),

      _ => throw new ArgumentException("Invalid configuration class.", nameof(configuration))
    };

    Connections.Add(connection.Id, connection);
    return connection;
  }, cancellationToken);
}

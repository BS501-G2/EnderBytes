namespace RizzziGit.EnderBytes.Services;

using Core;

public sealed partial class ConnectionService(Server server) : Server.SubService(server, "Connections")
{

  public Connection NewConnection(Parameters configuration, CancellationToken cancellationToken = default) => Run((cancellationToken) =>
  {
    Connection connection = configuration switch
    {
      Parameters.Basic internalConfiguration => new Connection.Basic(this, internalConfiguration),
      Parameters.Advanced internalConfiguration => new Connection.Advanced(this, internalConfiguration),
      Parameters.Internal internalConfiguration => new Connection.Internal(this, internalConfiguration),

      _ => throw new ArgumentException("Invalid configuration class.", nameof(configuration))
    };

    return connection;
  }, cancellationToken);
}

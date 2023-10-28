using System.Net;
using System.Net.Sockets;

namespace RizzziGit.EnderBytes.Protocols;

using Utilities;
using Connections;

public abstract class ProtocolConnection<P, PC> : Lifetime
  where P : Protocol<P, PC>
  where PC : ProtocolConnection<P, PC>
{
  public ProtocolConnection(P protocol, TcpClient client, Connection connection, IPEndPoint endPoint) : base($"{endPoint}")
  {
    Client = client;
    Connection = connection;
    Protocol = protocol;

    Protocol.Logger.Subscribe(Logger);

    Stopped += (_, _) => Connection.Stop();
  }

  public readonly P Protocol;
  public readonly TcpClient Client;
  public readonly Connection Connection;
}

public abstract class Protocol<P, PC> : Service
  where P : Protocol<P, PC>
  where PC : ProtocolConnection<P, PC>
{
  protected Protocol(ProtocolManager manager, string? name = null) : base(name, manager)
  {
    Manager = manager;
  }

  public readonly ProtocolManager Manager;

  protected abstract Task<PC> GetProtocolConnection(CancellationToken cancellationToken);

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      PC protocolConnection = await GetProtocolConnection(cancellationToken);
      protocolConnection.Start(cancellationToken);
    }
  }
}

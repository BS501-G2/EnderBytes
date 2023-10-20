using System.Net;
using System.Net.Sockets;

namespace RizzziGit.EnderBytes.Protocols;

using Utilities;
using Connections;

public abstract class ProtocolConnection<P, PC> : Lifetime
  where P : Protocol<P, PC>
  where PC : ProtocolConnection<P, PC>
{
  public ProtocolConnection(P protocol, TcpClient client, Connection connection, IPEndPoint endPoint)
  {
    Logger = new($"{endPoint}");
    Client = client;
    Connection = connection;
    Protocol = protocol;

    Protocol.Logger.Subscribe(Logger);
  }

  public readonly P Protocol;
  public readonly TcpClient Client;
  public readonly Connection Connection;
  public readonly Logger Logger;
}

public abstract class Protocol<P, PC> : Service
  where P : Protocol<P, PC>
  where PC : ProtocolConnection<P, PC>
{
  protected Protocol(ProtocolManager manager, string? name = null) : base(name)
  {
    Manager = manager;

    Manager.Logger.Subscribe(Logger);
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

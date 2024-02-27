using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Protocols;

using Services;
using Extras;

public sealed partial class FtpProtocol(ProtocolService service) : Protocol<FtpProtocol, FtpProtocol.Connection>(service, "FTP")
{
  public const string RETURN = "\r\n";

  protected override async IAsyncEnumerable<Connection> Listen([EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(Service.Server.Configuration.FtpAddress ?? new(IPAddress.Parse("0.0.0.0"), 8021));
    socket.Listen();

    try
    {
      while (true)
      {
        Socket client = await socket.AcceptAsync(cancellationToken);

        ConnectionEndPoint? remoteEndPoint = client.RemoteEndPoint != null
          ? new ConnectionEndPoint.Network((IPEndPoint)client.RemoteEndPoint)
          : null;

        ConnectionEndPoint? localEndPoint = client.LocalEndPoint != null
          ? new ConnectionEndPoint.Network((IPEndPoint)client.LocalEndPoint)
          : null;

        if (remoteEndPoint == null || localEndPoint == null)
        {
          Logger.Info("Rejected a client with an unknown remote address.");
          client.Close();

          continue;
        }

        yield return new(this, client, remoteEndPoint, localEndPoint);
      }
    }
    finally
    {
      socket.Shutdown(SocketShutdown.Both);
    }
  }
}

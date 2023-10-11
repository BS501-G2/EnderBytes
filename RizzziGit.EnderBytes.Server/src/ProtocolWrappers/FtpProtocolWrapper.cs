using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RizzziGit.EnderBytes.ProtocolWrappers;

using Collections;
using Connections;

public sealed class FtpConnection
{
  private readonly static WeakKeyDictionary<FtpProtocolWrapper, List<FtpConnection>> Contexts = new();
  private List<FtpConnection> Fellow
  {
    get
    {
      lock (Contexts)
      {
        if (Contexts.TryGetValue(Wrapper, out var value))
        {
          return value;
        }

        List<FtpConnection> contexts = [];
        Contexts.Add(Wrapper, contexts);
        return contexts;
      }
    }
  }

  public FtpConnection(in FtpProtocolWrapper wrapper, in Connection connection, in TcpClient client)
  {
    Wrapper = wrapper;
    Connection = connection;
    Client = client;

    Wrapper.Logger.Subscribe(Logger = new(client.Client.RemoteEndPoint?.ToString() ?? "Unknown"));
  }

  public readonly Logger Logger;
  public readonly FtpProtocolWrapper Wrapper;
  public readonly Connection Connection;
  public readonly TcpClient Client;

  public async void Handle(CancellationToken cancellationToken)
  {
    Fellow.Add(this);
    try
    {
      NetworkStream stream = Client.GetStream();
      BufferedStream bufferedStream = new(stream);

      while (Client.Connected)
      {
        cancellationToken.ThrowIfCancellationRequested();
        string command = "";
        do
        {
          byte[] bytes = new byte[4_096];
          command += Encoding.ASCII.GetString(bytes, 0, await bufferedStream.ReadAsync(bytes, cancellationToken));
        }
        while (!command.EndsWith("\r\n"));
      }
    }
    catch (Exception exception) { Logger.Log(Logger.LOGLEVEL_ERROR, $"{exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}"); }
    finally
    {
      Fellow.Remove(this);
    }
  }
}

public sealed class FtpProtocolWrapper(in EnderBytesServer server, in IPEndPoint ipEndPoint) : ProtocolWrapper("FTP", server)
{
  private readonly IPEndPoint IPEndPoint = ipEndPoint;
  private readonly TcpListener Listener = new(ipEndPoint);

  protected override async Task OnRun(Func<CancellationToken, Task<ClientConnection>> getContextCallback, CancellationToken cancellationToken)
  {
    Logger.Log(Logger.LOGLEVEL_INFO, $"Listening on {IPEndPoint}");

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      TcpClient tcpClient = await Listener.AcceptTcpClientAsync(cancellationToken);
      Connection connection = await getContextCallback(cancellationToken);

      FtpConnection ftpConnection = new(this, connection, tcpClient);
      ftpConnection.Handle(cancellationToken);


    }
  }
}

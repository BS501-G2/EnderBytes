using System.Net.Sockets;
using System.Text;

namespace RizzziGit.EnderBytes.Protocols;

using Connections;
using Services;
using Extras;

public sealed partial class FtpProtocol
{
  public new sealed partial class Connection(FtpProtocol protocol, Socket socket, ConnectionEndPoint remoteEndPoint, ConnectionEndPoint localEndPoint) : Protocol<FtpProtocol, Connection>.Connection(protocol)
  {
    private delegate Task SendHandler(int code, string message, CancellationToken cancellationToken);

    public ProtocolService Service => Protocol.Service;
    public override ConnectionEndPoint LocalEndPoint => localEndPoint;
    public override ConnectionEndPoint RemoteEndPoint => remoteEndPoint;

    public readonly Socket Socket = socket;

    private async Task Send(Reply reply, CancellationToken cancellationToken)
    {
      Protocol.Logger.Log(Framework.Logging.LogLevel.Debug, $"[{RemoteEndPoint} <- SERVER] {reply.Code} {reply.Message}");
      await Socket.SendAsync(Encoding.ASCII.GetBytes($"{reply.Code} {reply.Message}{RETURN}"), cancellationToken);
    }

    private BasicConnection? UnderlyingConnection;
    private string CommandBuffer = "";
    private long LastCommandTimestamp = 0;

    public override async Task Handle(CancellationToken cancellationToken)
    {
      using Socket socket = Socket;

      BasicConnection.ConnectionConfiguration connectionConfiguration = new(RemoteEndPoint, LocalEndPoint);
      BasicConnection connection = UnderlyingConnection = Service.Server.ConnectionService.NewConnection(connectionConfiguration, cancellationToken);

      Task monitorTask;
      Task loopTask;

      using CancellationTokenSource cancellationTokenSource = new();
      using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationTokenSource.Token, cancellationToken
      );

      {
        Task exitedTask = await Task.WhenAny(
          monitorTask = RunCommandTimeoutMonitor(linkedCancellationTokenSource.Token),
          loopTask = run(linkedCancellationTokenSource.Token)
        );

        cancellationTokenSource.Cancel();
        async Task run(CancellationToken cancellationToken)
        {
          await Send(new(220, "Welcome to EnderBytes FTP Server."), cancellationToken);
          await RunCommandLoop(cancellationToken);
        }
      }

      try { await Task.WhenAll(monitorTask, loopTask); }
      catch { }
      finally
      {

      }
    }

    private async Task RunCommandLoop(CancellationToken cancellationToken)
    {
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        int commandBufferReturnIndex;
        do
        {
          byte[] buffer = new byte[4096];
          int bufferLength = await Socket!.ReceiveAsync(buffer, cancellationToken);
          if (bufferLength == 0)
          {
            return;
          }

          lock (this)
          {
            CommandBuffer += Encoding.ASCII.GetString(buffer, 0, bufferLength);
          }
        }
        while ((commandBufferReturnIndex = CommandBuffer.IndexOf(RETURN)) < 0);

        string command;
        lock (this)
        {
          command = CommandBuffer[0..commandBufferReturnIndex];
          CommandBuffer = CommandBuffer[(commandBufferReturnIndex + RETURN.Length)..];

          LastCommandTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        {
          Command? parsedCommand = Command.Parse(command);
          Protocol.Logger.Log(Framework.Logging.LogLevel.Debug, $"[{RemoteEndPoint} -> SERVER] {(parsedCommand is Command.PASS ? "PASS <censored>" : command)}");
          await Send(await HandleCommand(parsedCommand, cancellationToken), cancellationToken);
        }
      }
    }

    private async Task RunCommandTimeoutMonitor(CancellationToken cancellationToken)
    {
      long timeElapsedSinceLastCommand() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LastCommandTimestamp;

      while (true)
      {
        await Task.Delay(1000, cancellationToken);

        if (timeElapsedSinceLastCommand() >= 30000)
        {
          await Send(new(CommandBuffer.Length != 0 ? 503 : 421, "Connection timed out."), cancellationToken);
        }
      }
    }
  }
}

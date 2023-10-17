using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RizzziGit.EnderBytes.Protocols;

using Buffer;
using Connections;

public abstract record FileTransferProtocolFormCode()
{
  public sealed record N() : FileTransferProtocolFormCode();
  public sealed record T() : FileTransferProtocolFormCode();
  public sealed record C() : FileTransferProtocolFormCode();
}

public abstract record FileTransferProtocolTypeCode()
{
  public sealed record A(FileTransferProtocolFormCode FormCode) : FileTransferProtocolTypeCode();
  public sealed record E(FileTransferProtocolFormCode FormCode) : FileTransferProtocolTypeCode();
  public sealed record I() : FileTransferProtocolTypeCode();
  public sealed record L(int ByteSize) : FileTransferProtocolTypeCode();
}

public abstract record FileTransferProtocolCommand()
{
  public static FileTransferProtocolCommand? Parse(string[] command)
  {
    if (command[0] == "USER")
    {
      string? username = command.ElementAtOrDefault(1);

      if (username == null)
      {
        return null;
      }

      return new USER(username);
    }

    return new Unknown(string.Join(' ', command));
  }

  public sealed record USER(string Username) : FileTransferProtocolCommand();
  public sealed record PASS(string Password) : FileTransferProtocolCommand();
  public sealed record ACCT(string AccountInformation) : FileTransferProtocolCommand();
  public sealed record CWD(string Pathname) : FileTransferProtocolCommand();
  public sealed record CDUP() : FileTransferProtocolCommand();
  public sealed record QUIT() : FileTransferProtocolCommand();
  public sealed record REIN() : FileTransferProtocolCommand();
  public sealed record PORT(IPEndPoint EndPoint) : FileTransferProtocolCommand();
  public sealed record PASV() : FileTransferProtocolCommand();
  public sealed record TYPE(string TypeCode) : FileTransferProtocolCommand();
  public sealed record STRU() : FileTransferProtocolCommand();
  public sealed record APPE(string Pathname) : FileTransferProtocolCommand();
  public sealed record ALLO(decimal Size) : FileTransferProtocolCommand();
  public sealed record REST(int Marker) : FileTransferProtocolCommand();
  public sealed record RNFR(string Pathname) : FileTransferProtocolCommand();
  public sealed record RNTO(string Pathname) : FileTransferProtocolCommand();
  public sealed record ABOR() : FileTransferProtocolCommand();
  public sealed record DELE(string Pathname) : FileTransferProtocolCommand();
  public sealed record RMD(string Pathname) : FileTransferProtocolCommand();
  public sealed record MKD(string Pathname) : FileTransferProtocolCommand();
  public sealed record PWD() : FileTransferProtocolCommand();
  public sealed record LIST(string? Pathname) : FileTransferProtocolCommand();
  public sealed record NLST(string? Pathname) : FileTransferProtocolCommand();
  public sealed record SITE(string Site) : FileTransferProtocolCommand();
  public sealed record SYST() : FileTransferProtocolCommand();
  public sealed record STAT(string? Pathname) : FileTransferProtocolCommand();
  public sealed record HELP(string? Command) : FileTransferProtocolCommand();
  public sealed record NOOP() : FileTransferProtocolCommand();
  public sealed record Unknown(string RawCommand) : FileTransferProtocolCommand();
}

public sealed record FileTransferProtocolReply(short Code, string Message)
{
  public FileTransferProtocolReply(short Code) : this(Code, Code switch
  {
    331 => "Username okay, need password",
    _ => ""
  })
  { }
}

public sealed class FileTransferProtocolConnection
{
  public FileTransferProtocolConnection(FileTransferProtocol fileTransferProtocol, ClientConnection connection, IPEndPoint remoteEndPoint)
  {
    Connection = connection;
    Logger = new(remoteEndPoint.ToString());
    IPEndPoint = remoteEndPoint;
    Protocol = fileTransferProtocol;
    ReplyCallback = (reply) => throw new NotImplementedException();

    fileTransferProtocol.Logger.Subscribe(Logger);
  }

  public readonly ClientConnection Connection;
  public readonly IPEndPoint IPEndPoint;
  public readonly Logger Logger;
  public readonly FileTransferProtocol Protocol;
  private Func<FileTransferProtocolReply, Task> ReplyCallback;

  public Task Reply(short code, string message) => ReplyCallback(new(code, message));
  public Task Reply(short code) => ReplyCallback(new(code));

  private string? PendingUsername;

  private async Task HandleCommand(FileTransferProtocolCommand.USER userCommand, CancellationToken cancellationToken)
  {
    PendingUsername = userCommand.Username;
    await Reply(331);
  }

  // private async

  private async Task Handle(FileTransferProtocolCommand? command, CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Verbose, $"> {command}");

    switch (command)
    {
      case FileTransferProtocolCommand.USER userCommand: await HandleCommand(userCommand, cancellationToken); break;
      case FileTransferProtocolCommand.Unknown: await Reply(500); break;
      default: await Reply(501); break;
    }
  }

  public async Task Handle(TcpClient client, CancellationToken cancellationToken)
  {
    NetworkStream stream = client.GetStream();
    using StreamReader streamReader = new(stream, Encoding.ASCII);
    using StreamWriter streamWriter = new(stream, Encoding.ASCII)
    {
      AutoFlush = true
    };

    var oldReply = ReplyCallback;
    ReplyCallback = async (reply) =>
    {
      Logger.Log(LogLevel.Verbose, $"< {reply.Code} {reply.Message}");

      await streamWriter.WriteLineAsync($"{reply.Code}{(reply.Message.Any() ? $"{reply.Message}" : "")}\r");
    };

    try
    {
      string? command = null;

      await Reply(220, "Halo.");
      await Reply(220);
      while ((command = await streamReader.ReadLineAsync()) != null)
      {
        await Handle(FileTransferProtocolCommand.Parse(command.Split(' ')), cancellationToken);
      }
    }
    catch { }
    finally
    {
      ReplyCallback = oldReply;
      Connection.Close();
      client.Close();
    }
  }
}

public sealed class FileTransferProtocol : Protocol
{
  public FileTransferProtocol(ProtocolManager manager) : base(manager, "FTP")
  {
    Listener = new(new IPEndPoint(manager.Server.Configuration.IpAddress, manager.Server.Configuration.FileTransferProtocolPort));

    Manager.Logger.Subscribe(Logger);
  }

  public readonly TcpListener Listener;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      TcpClient client = await Listener.AcceptTcpClientAsync(cancellationToken);
      IPEndPoint? endPoint = (IPEndPoint?)client.Client.RemoteEndPoint;
      if (endPoint == null)
      {
        client.Close();
        continue;
      }

      Logger.Log(LogLevel.Info, $"New FTP Client: {endPoint}");
      FileTransferProtocolConnection connection = new(this, await Manager.Server.Connections.GetClientConnection(cancellationToken), endPoint);
      _ = connection.Handle(client, cancellationToken);
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    Listener.Start();

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception)
  {
    Listener.Stop();

    return Task.CompletedTask;
  }
}

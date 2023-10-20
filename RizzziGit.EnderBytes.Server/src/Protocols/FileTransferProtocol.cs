using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RizzziGit.EnderBytes.Protocols.FileTransfer;

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
    else if (command[0] == "PASS")
    {
      string? password = command.ElementAtOrDefault(1);

      if (password == null)
      {
        return null;
      }

      return new PASS(password);
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

public sealed class FileTransferProtocolConnection(FileTransferProtocol protocol, TcpClient client, Connection connection, IPEndPoint endPoint) : ProtocolConnection<FileTransferProtocol, FileTransferProtocolConnection>(protocol, client, connection, endPoint)
{
  private Func<FileTransferProtocolReply, Task> ReplyCallback = (reply) => throw new NotImplementedException();

  public Task Reply(short code, string message) => ReplyCallback(new(code, message));
  public Task Reply(short code) => ReplyCallback(new(code));

  private string? PendingUsername;

  private async Task HandleCommand(FileTransferProtocolCommand.USER userCommand, CancellationToken cancellationToken)
  {
    if (string.Compare(userCommand.Username, "anonymous", true) == 0)
    {
      await Reply(332);

      return;
    }

    PendingUsername = userCommand.Username;
    await Reply(331);
  }

  private async Task HandleCommand(FileTransferProtocolCommand.PASS passCommand, CancellationToken cancellationToken)
  {
    if (PendingUsername != null)
    {
      string username = PendingUsername;
      string password = passCommand.Password;

      PendingUsername = null;
      if (await Connection.Execute(new Connection.Request.Login(username, password)) is Connection.Response.Ok)
      {
        await Reply(230);
        return;
      }
    }

    await Reply(430);
  }

  private async Task Handle(FileTransferProtocolCommand? rawCommand, CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Verbose, $"> {rawCommand}");

    switch (rawCommand)
    {
      case FileTransferProtocolCommand.USER command: await HandleCommand(command, cancellationToken); break;
      case FileTransferProtocolCommand.PASS command: await HandleCommand(command, cancellationToken); break;
      case FileTransferProtocolCommand.Unknown: await Reply(500); break;
      default: await Reply(501); break;
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    using NetworkStream stream = Client.GetStream();
    using StreamReader streamReader = new(stream, Encoding.ASCII);
    using StreamWriter streamWriter = new(stream, Encoding.ASCII)
    {
      AutoFlush = true
    };
    var oldReply = ReplyCallback;
    ReplyCallback = async (reply) =>
    {
      Logger.Log(LogLevel.Verbose, $"< REPLY {{ Code = {reply.Code}, Message = {reply.Message} }}");

      await streamWriter.WriteLineAsync($"{reply.Code}{(reply.Message.Any() ? $"{reply.Message}" : "")}\r");
    };

    try
    {
      string? command = null;

      await Reply(220, "Halo.");
      while ((command = await streamReader.ReadLineAsync(cancellationToken)) != null)
      {
        await Handle(FileTransferProtocolCommand.Parse(command.Split(' ')), cancellationToken);
      }
    }
    catch { }
    finally
    {
      ReplyCallback = oldReply;
    }
  }
}

public sealed class FileTransferProtocol(ProtocolManager manager) : Protocol<FileTransferProtocol, FileTransferProtocolConnection>(manager, "FTP")
{
  public readonly TcpListener Listener = new(new IPEndPoint(manager.Server.Configuration.IpAddress, manager.Server.Configuration.FileTransferProtocolPort));

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

  protected override async Task<FileTransferProtocolConnection> GetProtocolConnection(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      TcpClient client = await Listener.AcceptTcpClientAsync(cancellationToken);
      IPEndPoint? endPoint = (IPEndPoint?)client.Client.RemoteEndPoint;

      if (endPoint == null)
      {
        continue;
      }

      return new(this, client, await Manager.Server.Connections.GetClientConnection(cancellationToken), endPoint);
    }
  }
}

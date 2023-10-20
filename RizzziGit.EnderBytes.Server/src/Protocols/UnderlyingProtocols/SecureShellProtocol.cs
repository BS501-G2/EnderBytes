using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Protocols.SecureShell;

using Connections;

public enum SecureShellProtocolMessage : byte
{
  Disconnect = 1,
  Ignore = 2,
  Unimplemented = 3,
  Debug = 4,
  ServiceRequest = 5,
  ServiceAccept = 6,
  KexInit = 20,
  NewKeys = 21,
  UserAuthenticationRequest = 50,
  UserAuthenticationFailure = 51,
  UserAuthenticationSuccess = 52,
  UserAuthenticationBanner = 53,
  GlobalRequest = 80,
  RequestSuccess = 81,
  RequestFailure = 82,
  ChannelOpen = 90,
  ChannelOpenConfirmation = 91,
  ChannelOpenFailure = 92,
  ChannelWindowAdjust = 93,
  ChannelData = 94,
  ChannelExtendData = 95,
  ChannelEof = 96,
  ChannelClose = 97,
  ChannelRequest = 98,
  ChannelSuccess = 99,
  ChannelFailure = 100
}

public enum SecureShellProtocolDisconnectReason : byte
{
  HostNotAllowedToConnect = 1,
  ProtocolError = 2,
  KeyExchangeFailed = 3,
  Reserved = 4,
  MacError = 5,
  CompressionError = 6,
  ServiceNotAvailable = 7,
  ProtocolVersionNotSupported = 8,
  HostKeyNotVerifiable = 9,
  ConnectionLost = 10,
  ByApplication = 11,
  TooManyConnections = 12,
  AuthenticationCancelledByUser = 13,
  NoMoreAuthMethodsAvailable = 14,
  IllegalUserName = 15,
}

public sealed record ClientInformation(string ProtocolVersion, string ClientVersion, string? Comment = null)
{
  public static ClientInformation? Parse(string clientInformation)
  {
    string[] split = clientInformation.Trim().Split(' ');

    string? comment = split.ElementAtOrDefault(1);
    string? versions = split.ElementAtOrDefault(0);

    if (versions == null)
    {
      return null;
    }

    string[] versionsSplit = versions.Split('-');

    string? protocolVersion = versionsSplit.ElementAtOrDefault(1);
    if (protocolVersion == null)
    {
      return null;
    }

    string? clientVersion = versionsSplit.ElementAtOrDefault(2);
    if (clientVersion == null)
    {
      return null;
    }

    return new(protocolVersion, clientVersion, comment);
  }
}

public sealed record SecureShellProtocolPacket() { }

public sealed class SecureShellProtocolConnection(SecureShellProtocol protocol, TcpClient client, Connection connection, IPEndPoint endPoint) : ProtocolConnection<SecureShellProtocol, SecureShellProtocolConnection>(protocol, client, connection, endPoint)
{
  private NetworkStream? Stream;
  private StreamReader? StreamReader;
  private StreamWriter? StreamWriter;

  private ICryptoTransform? EncryptedReader;
  private ICryptoTransform? EncryptedWriter;

  public ClientInformation? ServerInformation { get; private set; }
  public ClientInformation? ClientInformation { get; private set; }

  private async Task SendClientInformation(ClientInformation clientInformation, CancellationToken cancellationToken)
  {
    if (ServerInformation != null)
    {
      throw new InvalidOperationException("Server information already sent.");
    }

    Logger.Log(LogLevel.Verbose, $"< {clientInformation}");
    await StreamWriter!.WriteLineAsync($"SSH-{clientInformation.ProtocolVersion}-{clientInformation.ClientVersion}{(clientInformation.Comment != null ? $" {clientInformation.Comment}" : "")}\r");
  }

  private async Task SendClientInformation(CancellationToken cancellationToken)
  {
    Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    string name = assembly.GetName().Name ?? "EnderBytes";
    string version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion ?? "1.0";

    await SendClientInformation(new("2.0", $"{name}_{version}", "For file system access purposes only."), cancellationToken);
  }

  private async Task<bool> ReceiveClientInformation(CancellationToken cancellationToken)
  {
    if (ClientInformation != null)
    {
      throw new InvalidOperationException("Client information already received.");
    }

    ClientInformation? clientInformation;
    {
      string? rawClientInformation;
      if (
        ((rawClientInformation = await StreamReader!.ReadLineAsync(cancellationToken)) == null) ||
        (rawClientInformation.Length >= 255)
      )
      {
        return false;
      }

      clientInformation = ClientInformation.Parse(rawClientInformation);
    }

    if (clientInformation == null)
    {
      return false;
    }

    ClientInformation = clientInformation;
    return true;
  }

  private async Task<(byte[] packet, byte[]? mac)> ReceivePacket(CancellationToken cancellationToken) {
    byte[] byteLength = new byte[4];
    await Stream!.ReadExactlyAsync(byteLength, cancellationToken);

    throw new NotImplementedException();
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await using NetworkStream stream = Stream = Client.GetStream();

    using StreamReader reader = StreamReader = new(stream, Encoding.ASCII);
    await using StreamWriter writer = StreamWriter = new(stream, Encoding.ASCII);

    try
    {
      await SendClientInformation(cancellationToken);
      if (!await ReceiveClientInformation(cancellationToken))
      {
        return;
      }

      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();
        // byte[] packet = await ReceivePacket(cancellationToken);
      }
    }
    finally
    {
      Stream = null;
      StreamReader = null;
      StreamWriter = null;
    }
  }
}

public sealed class SecureShellProtocol(ProtocolManager manager) : Protocol<SecureShellProtocol, SecureShellProtocolConnection>(manager, "SSH")
{
  private readonly TcpListener Listener = new(new IPEndPoint(manager.Server.Configuration.IpAddress, manager.Server.Configuration.SecureShellProtocolPort));

  protected override async Task<SecureShellProtocolConnection> GetProtocolConnection(CancellationToken cancellationToken)
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

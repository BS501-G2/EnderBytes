using System.Net;

namespace RizzziGit.EnderBytes;

using Resources;
using Connections;
using Sessions;
using RizzziGit.EnderBytes.Protocols;

public struct ServerConfiguration()
{
  public string DatabasePath = Path.Join(Environment.CurrentDirectory, ".db");

  public IPAddress IpAddress = IPAddress.Parse("0.0.0.0");

  public int FileTransferProtocolPort = 8021;

  public int DefaultUserAuthenticationPayloadHashIterationCount = 1000;
  public int DefaultBlobStorageFileBufferSize = 256 * 1024;
}

public sealed class Server : Service
{
  public Server() : this(new()) { }
  public Server(ServerConfiguration configuration) : base("Server")
  {
    Configuration = configuration;
    Resources = new(this);
    Sessions = new(this);
    Connections = new(this);
    Protocols = new(this);
  }

  public readonly ServerConfiguration Configuration;
  public readonly MainResourceManager Resources;
  public readonly SessionManager Sessions;
  public readonly ConnectionManager Connections;
  public readonly ProtocolManager Protocols;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Resources.Start();
    await Sessions.Start();
    await Connections.Start();
    await Protocols.Start();
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Info, "Server is now running.");
    await (await WatchDog([Resources, Sessions, Connections, Protocols], cancellationToken)).task;
  }

  protected override async Task OnStop(Exception? exception)
  {
    Logger.Log(LogLevel.Info, "server is shutting down.");
    await Protocols.Stop();
    await Connections.Stop();
    await Sessions.Stop();
    await Resources.Stop();
    Logger.Log(LogLevel.Info, "Server has shut down.");
  }
}

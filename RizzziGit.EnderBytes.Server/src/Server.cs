using System.Net;

namespace RizzziGit.EnderBytes;

using Resources;
using Connections;
using Sessions;
using Protocols;
using ArtificialIntelligence;
using StoragePools;
using RizzziGit.EnderBytes.Keys;

public struct ServerConfiguration()
{
  private static readonly string BASE_PATH = Environment.CurrentDirectory;

  public string DatabasePath = Path.Join(BASE_PATH, ".db");
  public string BlobPath = Path.Join(BASE_PATH, ".db", "BlobStorageData");

  #if WHISPER_CPP
  // Get the dataset from the source: https://huggingface.co/openai/whisper-large-v2
  // GGML: https://huggingface.co/4bit/whisper-large-v2-ggml/resolve/main/ggml-large-v2.bin
  public string WhisperDatasetPath = Path.Join(BASE_PATH, ".ai", "WhisperDataset.bin");
  #endif

  public IPAddress IpAddress = IPAddress.Parse("0.0.0.0");

  public int FileTransferProtocolPort = 8021;
  public int SecureShellProtocolPort = 8022;

  public int DefaultUserAuthenticationPayloadHashIterationCount = 1000;
  public int DefaultBlobStorageFileBufferSize = 256 * 1024;

  public int MaxCachedDataSize = 1024 * 1024 * 2;
}

public sealed class Server : Service
{
  public Server() : this(new()) { }
  public Server(ServerConfiguration configuration) : base("Server")
  {
    Configuration = configuration;
    Resources = new(this);
    KeyGenerator = new(this);
    Sessions = new(this);
    Connections = new(this);
    StoragePools = new(this);
    Protocols = new(this);
    ArtificialIntelligence = new(this);
  }

  public readonly ServerConfiguration Configuration;
  public readonly MainResourceManager Resources;
  public readonly KeyGenerator KeyGenerator;
  public readonly SessionManager Sessions;
  public readonly ConnectionManager Connections;
  public readonly ProtocolManager Protocols;
  public readonly StoragePoolManager StoragePools;
  public readonly ArtificialIntelligenceManager ArtificialIntelligence;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Resources.Start();
    await KeyGenerator.Start();
    await Sessions.Start();
    await Connections.Start();
    await StoragePools.Start();
    await Protocols.Start();
    // await ArtificialIntelligence.Start();
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Info, "Server is now running.");
    await (await WatchDog([Resources, Sessions, Connections, Protocols, KeyGenerator], cancellationToken)).task;
  }

  protected override async Task OnStop(Exception? exception)
  {
    Logger.Log(LogLevel.Info, "server is shutting down.");
    await Protocols.Stop();
    await StoragePools.Stop();
    await Connections.Stop();
    await Sessions.Stop();
    await KeyGenerator.Stop();
    await Resources.Stop();
    // await ArtificialIntelligence.Stop();
    Logger.Log(LogLevel.Info, "Server has shut down.");
  }
}

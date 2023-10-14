namespace RizzziGit.EnderBytes;

using Resources;

public struct ServerConfig()
{
  public string DatabasePath = Path.Join(Environment.CurrentDirectory, ".db");
  public int DefaultUserAuthenticationResourceIterationCount = 1000;
}

public sealed class Server
{
  public Server() : this(new()) { }
  public Server(ServerConfig config)
  {
    Logger = new("Server");
    Config = config;
    Resources = new(this);
  }

  public readonly Logger Logger;
  public readonly ServerConfig Config;
  public readonly MainResourceManager Resources;

  public async Task Run(TaskCompletionSource onReady, CancellationToken cancellationToken)
  {
    await Resources.Run(onReady, cancellationToken);
  }
}

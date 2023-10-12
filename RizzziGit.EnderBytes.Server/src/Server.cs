namespace RizzziGit.EnderBytes;

using Resources;

public struct ServerConfig()
{
  public string DatabasePath = Path.Join(Environment.CurrentDirectory, ".db");
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

  public async Task Run(CancellationToken cancellationToken)
  {
    await Resources.Run(cancellationToken);
  }
}

namespace RizzziGit.EnderBytes.Core;

using Framework.Services;

public sealed partial class Server
{
  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }
}

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Configuration
  {
    public sealed class Internal() : Configuration;
  }

  public abstract partial class Connection<C>
  {
    public sealed class Internal : Connection<Configuration.Internal>
    {

    }
  }
}

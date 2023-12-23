namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Configuration
  {
    public sealed class Advanced() : Configuration;
  }

  public abstract partial class Connection<C>
  {
    public sealed class Advanced : Connection<Configuration.Advanced>
    {

    }
  }
}

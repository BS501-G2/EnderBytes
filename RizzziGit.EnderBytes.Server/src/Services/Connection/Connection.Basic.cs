
namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    public sealed record Basic() : Configuration;
  }

  public abstract partial class Connection
  {
    public sealed class Basic(Configuration.Basic configuration) : Connection(configuration)
    {
      public new readonly Configuration.Basic Configuration = configuration;
    }
  }
}

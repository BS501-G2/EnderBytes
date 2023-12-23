namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

public sealed partial class ConnectionService
{
  public interface IConnection;

  public abstract partial class Configuration
  {
    private Configuration() { }
  }

  public abstract partial class Connection<C> : Lifetime, IConnection
    where C : Configuration
  {
    private Connection() : base("Connection") { }

    public UserService.Session? Session { get; private set; } = null;
  }
}

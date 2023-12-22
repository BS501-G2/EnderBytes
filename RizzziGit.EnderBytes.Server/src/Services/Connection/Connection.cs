namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract partial class Connection
  {
    public UserService.Session? Session { get; private set; } = null;
  }
}

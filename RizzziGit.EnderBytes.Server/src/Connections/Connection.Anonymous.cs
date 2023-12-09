namespace RizzziGit.EnderBytes.Connections;

public abstract partial class Connection
{
  public sealed partial class Anonymous(ConnectionManager manager, long id) : Connection(manager, id)
  {
  }
}

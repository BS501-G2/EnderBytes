namespace RizzziGit.EnderBytes.Connections;

public abstract partial class Connection
{
  public sealed partial class Internal(ConnectionManager manager, long id) : Connection(manager, id)
  {
    
  }
}

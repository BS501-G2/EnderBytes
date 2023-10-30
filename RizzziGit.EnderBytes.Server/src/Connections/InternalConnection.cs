namespace RizzziGit.EnderBytes.Connections;

public sealed class InternalConnection : Connection
{
  public InternalConnection(ConnectionManager manager, ulong id) : base(manager, id)
  {
  }
}

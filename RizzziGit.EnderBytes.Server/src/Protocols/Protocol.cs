namespace RizzziGit.EnderBytes.Protocols;

public abstract class Protocol : Service
{
  protected Protocol(ProtocolManager manager, string? name = null) : base(name)
  {
    Manager = manager;
  }

  public readonly ProtocolManager Manager;
}

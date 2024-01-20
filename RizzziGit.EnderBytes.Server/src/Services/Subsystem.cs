namespace RizzziGit.EnderBytes.Services.Subsystem;

public abstract class Subsystem
{
  public sealed class SubsystemService(Server server) : Server.SubService(server, "Subsystems")
  {

  }
}

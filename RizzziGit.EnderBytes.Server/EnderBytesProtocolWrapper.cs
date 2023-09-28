namespace RizzziGit.EnderBytes;

public abstract class EnderBytesProtocolWrapper : EnderBytesServer.ProtocolWrapper
{
  protected EnderBytesProtocolWrapper(string name, EnderBytesServer server) : base(server)
  {
    Name = name;
  }

  public readonly string Name;
}

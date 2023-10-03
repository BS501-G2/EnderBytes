namespace RizzziGit.EnderBytes.ProtocolWrappers;

public abstract class ProtocolWrapper : EnderBytesServer.ProtocolWrapper
{
  protected ProtocolWrapper(string name, EnderBytesServer server) : base(server)
  {
    Name = name;
    Logger = new($"Protocol Wrapper ({name})");

    server.Logger.Subscribe(Logger);
  }

  public readonly string Name;
  public readonly Logger Logger;
}

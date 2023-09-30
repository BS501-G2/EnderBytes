namespace RizzziGit.EnderBytes;

public abstract class EnderBytesProtocolWrapper : EnderBytesServer.ProtocolWrapper
{
  protected EnderBytesProtocolWrapper(string name, EnderBytesServer server) : base(server)
  {
    Name = name;
    Logger = new($"Protocol Wrapper ({name})");

    server.Logger.Subscribe(Logger);
  }

  ~EnderBytesProtocolWrapper()
  {
    Server.Logger.Unsubscribe(Logger);
  }

  public readonly string Name;
  public readonly EnderBytesLogger Logger;
}

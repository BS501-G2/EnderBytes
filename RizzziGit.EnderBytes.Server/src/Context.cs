namespace RizzziGit.EnderBytes;

using Resources;

public class Context : EnderBytesServer.Context
{
  public Context(EnderBytesServer server, CancellationToken cancellationToken) : base(server, cancellationToken)
  {
    Logger = new($"Context (#{ID})");

    Server.Logger.Subscribe(Logger);
  }

  public UserResource? CurrentUser { get; private set; }
  public readonly Logger Logger;
}

namespace RizzziGit.EnderBytes;

using Resources;

public class EnderBytesContext : EnderBytesServer.Context
{
  public EnderBytesContext(EnderBytesServer server, CancellationToken cancellationToken) : base(server, cancellationToken)
  {
    Logger = new($"Context (#{ID})");

    Server.Logger.Subscribe(Logger);
  }

  ~EnderBytesContext()
  {
    Server.Logger.Unsubscribe(Logger);
  }

  public UserResource? CurrentUser { get; private set; }
  public readonly EnderBytesLogger Logger;
}

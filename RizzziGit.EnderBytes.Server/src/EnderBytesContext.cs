namespace RizzziGit.EnderBytes;

using Resources;

public class EnderBytesContext(EnderBytesServer server, CancellationToken cancellationToken) : EnderBytesServer.Context(server, cancellationToken)
{
  public UserResource? CurrentUser { get; private set; }
}

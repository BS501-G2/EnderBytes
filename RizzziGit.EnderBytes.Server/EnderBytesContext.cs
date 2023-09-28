namespace RizzziGit.EnderBytes;

public class EnderBytesContext(EnderBytesServer server, CancellationToken cancellationToken) : EnderBytesServer.Context(server, cancellationToken)
{
}

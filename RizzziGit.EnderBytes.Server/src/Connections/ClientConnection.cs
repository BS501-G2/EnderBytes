namespace RizzziGit.EnderBytes.Connections;

public class ClientConnection(EnderBytesServer server) : Connection(server)
{
  protected override async Task<ConnectionResponse> HandleCommand(ConnectionCommand command, CancellationToken cancellationToken)
  {
    switch (command)
    {
      case ConnectionCommand.AuthenticateWithPassword authenticateWithPassword: return await HandleCommand(authenticateWithPassword, cancellationToken);

      default: return new(ConnectionResponse.CODE_INVALID_COMMAND);
    }
  }
}

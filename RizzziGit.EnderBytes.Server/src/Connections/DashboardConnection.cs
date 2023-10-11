namespace RizzziGit.EnderBytes.Connections;

public class DashboardConnection(EnderBytesServer server) : Connection(server)
{
  protected override async Task<ConnectionResponse> HandleCommand(ConnectionCommand command, CancellationToken cancellationToken)
  {
    switch (command)
    {
      case ConnectionCommand.AuthenticateWithPassword authenticateWithPasswordCommand: return await HandleCommand(authenticateWithPasswordCommand, cancellationToken);

      default: return new(ConnectionResponse.CODE_INVALID_COMMAND);
    }
  }
}

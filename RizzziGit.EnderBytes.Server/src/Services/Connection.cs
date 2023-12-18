namespace RizzziGit.EnderBytes.Services;

public sealed class ConnectionService(Server server) : Service("Connections", server)
{
  public readonly Server Server = server;

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

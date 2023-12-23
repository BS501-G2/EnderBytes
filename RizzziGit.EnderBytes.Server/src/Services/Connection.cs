namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

public sealed partial class ConnectionService(Server server) : Service("Connections", server)
{
  public readonly Server Server = server;

  private readonly WaitQueue<(TaskCompletionSource<Connection<Configuration>> source, Configuration configuration)> WaitQueue = new(0);

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

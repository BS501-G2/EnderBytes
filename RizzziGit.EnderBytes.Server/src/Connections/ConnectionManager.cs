namespace RizzziGit.EnderBytes.Connections;

using Collections;

public sealed class ConnectionManager(Server server) : Service("Connections", server)
{
  public readonly Server Server = server;
  private WaitQueue<(TaskCompletionSource<Connection> source, bool isDashboard)> WaitQueue = new();

  public async Task<ClientConnection> GetClientConnection(CancellationToken cancellationToken)
  {
    TaskCompletionSource<Connection> source = new();
    await WaitQueue.Enqueue((source, false), cancellationToken);
    return (ClientConnection)await source.Task;
  }

  public async Task<DashboardConnection> GetDashboardConnection(CancellationToken cancellationToken)
  {
    TaskCompletionSource<Connection> source = new();
    await WaitQueue.Enqueue((source, true), cancellationToken);
    return (DashboardConnection)await source.Task;
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    try { WaitQueue.Dispose(); } catch { }

    WaitQueue = new();
    return Task.CompletedTask;
  }

  protected override async Task OnRun(CancellationToken serviceCancellationToken)
  {
    Logger.Log(LogLevel.Info, "Connection factory is now running.");
    ulong id = 0;
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();
      var (source, isDashboard) = await WaitQueue.Dequeue(serviceCancellationToken);

      Logger.Log(LogLevel.Verbose, $"New {(isDashboard ? "dashboard" : "client")} connection requested. (#{id})");
      Connection connection = isDashboard
        ? new DashboardConnection(this, id)
        : new ClientConnection(this, id);

      connection.Start(serviceCancellationToken);
      source.SetResult(connection);
      id++;
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    try { WaitQueue.Dispose(exception); } catch { }
    return Task.CompletedTask;
  }
}
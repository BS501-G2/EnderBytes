namespace RizzziGit.EnderBytes.Connections;

using Collections;

public sealed class ConnectionManager : Service
{
  public ConnectionManager(Server server) : base("Connections")
  {
    Server = server;
    WaitQueue = new();

    Server.Logger.Subscribe(Logger);
  }

  public readonly Server Server;
  private WaitQueue<(TaskCompletionSource<Connection> source, bool isDashboard)> WaitQueue;

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
    ulong id = 0;
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();
      var (source, isDashboard) = await WaitQueue.Dequeue(serviceCancellationToken);

      Logger.Log(LogLevel.Verbose, $"New connection requested. (#{id})");
      CancellationTokenSource cancellationTokenSource = new();
      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationTokenSource.Token,
        serviceCancellationToken
      );

      Connection connection = isDashboard
        ? new DashboardConnection(this, id, cancellationTokenSource)
        : new ClientConnection(this, id, cancellationTokenSource);

      Watchdog(connection.Run(linkedCancellationTokenSource.Token), cancellationTokenSource, linkedCancellationTokenSource);
      source.SetResult(connection);
    }
  }

  private static async void Watchdog(Task task, CancellationTokenSource cancellationTokenSource, CancellationTokenSource linkedCancellationTokenSource)
  {
    try
    {
      await task.WaitAsync(CancellationToken.None);
    }
    finally
    {
      linkedCancellationTokenSource.Dispose();
      cancellationTokenSource.Dispose();
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    WaitQueue.Dispose(exception);
    return Task.CompletedTask;
  }
}

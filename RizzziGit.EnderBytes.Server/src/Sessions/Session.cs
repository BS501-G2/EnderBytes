namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Utilities;
using Connections;

public class UserSession
{
  public UserSession(SessionManager manager, ulong id, CancellationTokenSource cancellationTokenSource, UserResource user)
  {
    Logger = new($"#{id}");
    Id = id;
    Manager = manager;
    CancellationTokenSource = cancellationTokenSource;
    User = user;
    TaskQueue = new();
    Connections = [];

    IsRunning = false;
    Manager.Logger.Subscribe(Logger);
  }

  ~UserSession() => Close();

  public readonly Logger Logger;
  public readonly SessionManager Manager;
  public readonly ulong Id;
  private readonly CancellationTokenSource CancellationTokenSource;
  private readonly TaskQueue TaskQueue;
  public readonly List<Connection> Connections;

  public readonly UserResource User;

  private async Task RunChecker(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (Connections.Count == 0)
      {
        Close();
      }

      await Task.Delay(1000, cancellationToken);
    }
  }

  public void Close()
  {
    try { CancellationTokenSource.Cancel(); } catch { }
  }

  public bool IsRunning { get; private set; }
  public async Task Run(CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Verbose, "Session task queue and checker started.");
    try
    {
      IsRunning = true;
      await Task.WhenAll(
        TaskQueue.Start(cancellationToken),
        RunChecker(cancellationToken)
      );
    }
    finally
    {
      IsRunning = false;
      Logger.Log(LogLevel.Verbose, "Session task queue and checker stopped.");
    }
  }
}

namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Utilities;
using Connections;

public class UserSession
{
  public UserSession(CancellationTokenSource cancellationTokenSource, UserResource user)
  {
    CancellationTokenSource = cancellationTokenSource;
    User = user;
    TaskQueue = new();
    Connections = [];

    IsRunning = false;
  }

  ~UserSession() => Close();

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
    }
  }
}

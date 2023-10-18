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
  private readonly List<Connection> Connections;

  public void AddConnection(Connection connection)
  {
    lock (Manager)
    {
      Connections.Add(connection);
    }
  }

  public void RemoveConnection(Connection connection)
  {
    lock (Manager)
    {
      Connections.Remove(connection);

      if (Connections.Count == 0)
      {
        Close();
      }
    }
  }

  public readonly UserResource User;

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
      await TaskQueue.Start(cancellationToken);
    }
    finally
    {
      IsRunning = false;
      Logger.Log(LogLevel.Verbose, "Session task queue and checker stopped.");
    }
  }
}

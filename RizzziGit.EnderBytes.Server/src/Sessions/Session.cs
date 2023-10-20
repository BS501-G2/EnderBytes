namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Utilities;
using Connections;

public class UserSession : Lifetime
{
  public UserSession(SessionManager manager, ulong id, UserResource user)
  {
    Logger = new($"#{id}");
    Id = id;
    Manager = manager;
    User = user;
    Connections = [];

    Manager.Logger.Subscribe(Logger);
  }

  public readonly Logger Logger;
  public readonly SessionManager Manager;
  public readonly ulong Id;
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
        Stop();
      }
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await Task.Delay(-1, cancellationToken);
  }

  public readonly UserResource User;
}

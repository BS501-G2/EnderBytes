namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Collections;
using Connections;

public sealed class SessionManager : Service
{
  public SessionManager(Server server) : base("Sessions")
  {
    Server = server;
    Sessions = new();
    WaitQueue = new();

    Server.Resources.Users.OnResourceDelete((transaction, resource) =>
    {
      lock (Sessions)
      {
        if (Sessions.TryGetValue(resource, out var session))
        {
          session.Stop();
        }
      }
    });

    Server.Logger.Subscribe(Logger);
  }

  public readonly Server Server;
  private readonly WeakDictionary<UserResource, UserSession> Sessions;
  private WaitQueue<(TaskCompletionSource<UserSession> source, UserResource user, Connection connection)> WaitQueue;

  public async Task<UserSession> GetUserSession(UserResource user, Connection connection, CancellationToken cancellationToken)
  {
    TaskCompletionSource<UserSession> source = new();
    await WaitQueue.Enqueue((source, user, connection), cancellationToken);
    return await source.Task;
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    try { WaitQueue.Dispose(); } catch { }
    WaitQueue = new();
    return Task.CompletedTask;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Info, "Session factory is now running.");
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var (source, user, connection) = await WaitQueue.Dequeue(cancellationToken);

      lock (this)
      {
        {
          if (Sessions.TryGetValue(user, out var session))
          {
            session.AddConnection(connection);
            source.SetResult(session);
            continue;
          }
        }

        {
          UserSession session = new(this, user);
          Sessions.Add(user, session);

          session.AddConnection(connection);
          session.Stopped += (_, _) =>
          {
            Sessions.Remove(user);
          };
          session.Start(cancellationToken);
          source.SetResult(session);
        }
      }
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    try { WaitQueue.Dispose(exception); } catch { }
    return Task.CompletedTask;
  }
}

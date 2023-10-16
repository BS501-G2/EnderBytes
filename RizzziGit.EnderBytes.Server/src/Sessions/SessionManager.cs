namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Collections;
using Connections;

public sealed class SessionManager : Service
{
  public SessionManager(Server server)
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
          Sessions.Remove(resource);
          session.Close();
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
    ulong id = 0;
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var (source, user, connection) = await WaitQueue.Dequeue(cancellationToken);

      CancellationTokenSource cancellationTokenSource = new();
      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationTokenSource.Token,
        cancellationToken
      );

      lock (Sessions)
      {
        {
          if (Sessions.TryGetValue(user, out var session))
          {
            session.Connections.Add(connection);
            source.SetResult(session);
            continue;
          }
        }

        {
          UserSession session = new(this, id, cancellationTokenSource, user);
          Sessions.Add(user, session);

          session.Connections.Add(connection);
          Watchdog(session.Run(linkedCancellationTokenSource.Token), cancellationTokenSource, linkedCancellationTokenSource);
          source.SetResult(session);
        }
      }

      id++;
    }
  }

  private static async void Watchdog(Task task, CancellationTokenSource cancellationTokenSource, CancellationTokenSource linkedCancellationTokenSource)
  {
    try
    {
      await task;
    }
    catch { }
    finally
    {
      linkedCancellationTokenSource.Dispose();
      cancellationTokenSource.Dispose();
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    try { WaitQueue.Dispose(exception); } catch { }
    return Task.CompletedTask;
  }
}

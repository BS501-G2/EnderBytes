namespace RizzziGit.EnderBytes.Sessions;

using Resources;
using Collections;
using Connections;

public sealed record UserSession(SessionManager Manager, UserResource User)
{
  public sealed record HashCache(UserAuthenticationResource UserAuthentication, byte[] Cache);

  public bool IsValid => User.IsValid;
  public readonly WeakKeyDictionary<Connection, HashCache> HashCaches = new();
}

public sealed class SessionManager : Service
{
  public SessionManager(Server server) : base("Sessions")
  {
    Server = server;
    Sessions = new();
    WaitQueue = new();

    Server.Resources.Users.OnResourceDelete((_, resource) =>
    {
      lock (this)
      {
        Sessions.Remove(resource);
      }
    });

    Server.Logger.Subscribe(Logger);
  }

  public readonly Server Server;
  private readonly WeakDictionary<UserResource, UserSession> Sessions;
  private WaitQueue<(TaskCompletionSource<UserSession> source, UserResource user, Connection connection, UserAuthenticationResource userAuthentication, byte[] hashCache)> WaitQueue;

  public async Task<UserSession> GetUserSession(UserResource user, Connection connection, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken)
  {
    TaskCompletionSource<UserSession> source = new();
    await WaitQueue.Enqueue((source, user, connection, userAuthentication, hashCache), cancellationToken);
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
      var (source, user, connection, userAuthentication, hashCache) = await WaitQueue.Dequeue(cancellationToken);

      lock (this)
      {
        UserSession session;
        if (Sessions.TryGetValue(user, out var s))
        {
          session = s;
          continue;
        }
        else
        {
          session = new(this, user);
          Sessions.Add(user, session);
        }

        session.HashCaches.AddOrUpdate(connection, new(userAuthentication, hashCache));
        source.SetResult(session);
      }
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    try { WaitQueue.Dispose(exception); } catch { }
    return Task.CompletedTask;
  }
}

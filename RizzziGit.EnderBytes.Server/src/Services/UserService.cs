using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Collections;
using Records;
using Utilities;

public enum UserAuthenticationType
{
  Password,
  Google,
  SessionToken
}

public sealed class UserService(Server server) : Server.SubService(server, "Users")
{
  private long LastSessionId = 0;

  public IMongoCollection<Record.User> Users => Server.GetCollection<Record.User>();

  private readonly WeakDictionary<long, GlobalSession> Sessions = [];
  private readonly WaitQueue<(TaskCompletionSource<GlobalSession> source, long userId)> WaitQueue = new();

  public sealed class GlobalSession(UserService service, long userId) : Lifetime($"User #{userId}")
  {
    public readonly UserService Service = service;
    public readonly long UserId = userId;

    private readonly WeakDictionary<long, Session> Sessions = [];
    private readonly WaitQueue<(TaskCompletionSource<Session> source, KeyGeneratorService.Transformer.UserKey transformer)> WaitQueue = new();

    public async Task<Session> Get(KeyGeneratorService.Transformer.UserKey transformer, long? sessionId = null)
    {
      if (transformer.PublicOnly)
      {
        throw new ArgumentException("Transformer cannot be public only.", nameof(transformer));
      }
      else if (sessionId != null && Sessions.TryGetValue((long)sessionId, out Session? value))
      {
        return value;
      }

      TaskCompletionSource<Session> source = new();
      await WaitQueue.Enqueue((source, transformer));
      return await source.Task;
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      await foreach (var (source, transformer) in WaitQueue)
      {
        try
        {
          lock (Sessions)
          {
            long sessionId = Service.LastSessionId++;

            Session session = Sessions[sessionId] = new(this, sessionId, transformer);
            session.Start(cancellationToken);

            source.TrySetResult(session);
          }
        }
        catch (Exception exception)
        {
          source.SetException(exception);
        }
      }
    }
  }

  public sealed class Session(GlobalSession global, long id, KeyGeneratorService.Transformer.UserKey transformer) : Lifetime($"#{id}", global)
  {
    public readonly GlobalSession Global = global;
    public readonly long Id = id;
    public readonly KeyGeneratorService.Transformer.UserKey Transformer = transformer;
  }

  public async Task<Session> GetSession(Record.User user, Record.UserAuthentication authentication, byte[] hashCache)
  {
    GlobalSession globalSession;
    {
      TaskCompletionSource<GlobalSession> globalSource = new();
      await WaitQueue.Enqueue((globalSource, user.Id));
      globalSession = await globalSource.Task;
    }

    if (!authentication.HashMatches(hashCache))
    {
      throw new ArgumentException("Invalid hash cache.", nameof(hashCache));
    }

    Record.UserKey userKey = await Server.KeyGeneratorService.GetUserKey(user, authentication) ?? throw new InvalidOperationException("No user key available for specified authentication.");
    KeyGeneratorService.Transformer.UserKey userKeyTransformer = Server.KeyGeneratorService.GetTransformer(userKey, hashCache);
    return await globalSession.Get(userKeyTransformer);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, userId) in WaitQueue)
    {
      try
      {
        lock (Sessions)
        {
          if (!Sessions.TryGetValue(userId, out var value))
          {
            (value = new(this, userId)).Start(cancellationToken);
          }

          source.SetResult(Sessions[userId] = value);
        }
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    Users.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (Sessions.TryGetValue(change.FullDocument.Id, out GlobalSession? session))
      {
        session.Stop();
      }
    }, cancellationToken);

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

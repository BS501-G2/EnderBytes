using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Collections;
using Records;
using Utilities;

public enum UserAuthenticationType : byte { Password, SessionToken }

public sealed partial class UserService(Server server) : Server.SubService(server, "Users")
{
  public IMongoCollection<Record.User> UserRecords => Server.GetCollection<Record.User>();

  private readonly WeakDictionary<long, GlobalSession> Sessions = [];
  private readonly WaitQueue<(TaskCompletionSource<GlobalSession> source, long userId)> WaitQueue = new();

  public async Task<Session> GetSession(Record.User user, KeyGeneratorService.Transformer.UserAuthentication transformer)
  {
    GlobalSession globalSession;
    {
      TaskCompletionSource<GlobalSession> globalSource = new();
      await WaitQueue.Enqueue((globalSource, user.Id));
      globalSession = await globalSource.Task;

      if (transformer.UserId != user.Id || transformer.PublicOnly)
      {
        throw new ArgumentException("Transformer does not match the user id.", nameof(transformer));
      }

      return await globalSession.Get(transformer, LastSessionId++);
    }
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
    UserRecords.BeginWatching((change) =>
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

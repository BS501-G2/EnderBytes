namespace RizzziGit.EnderBytes.Services;

using Collections;
using Utilities;

public sealed partial class UserService
{
  private long LastSessionId = 0;

  public sealed class GlobalSession(UserService service, long userId) : Lifetime($"User #{userId}")
  {
    public readonly UserService Service = service;
    public readonly long UserId = userId;

    private readonly WeakDictionary<long, Session> Sessions = [];
    private readonly WaitQueue<(TaskCompletionSource<Session> source, KeyGeneratorService.Transformer.UserAuthentication transformer)> WaitQueue = new();

    public async Task<Session> Get(KeyGeneratorService.Transformer.UserAuthentication transformer, long? sessionId = null)
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
}

namespace RizzziGit.EnderBytes;

using Buffer;
using RizzziGit.EnderBytes.Resources;

public static class Program
{
  public static void RegisterCancelKey(CancellationTokenSource source)
  {
    void onCancel(object? sender, ConsoleCancelEventArgs args)
    {
      try { source.Cancel(); } catch { }

      Console.CancelKeyPress -= onCancel;
    }
    Console.CancelKeyPress += onCancel;
  }

  private static long Time = 0;
  private static long RandomHits = 0;
  public static async Task A(EnderBytesServer server, CancellationToken cancellationToken)
  {
    while (true)
    {
      string username = Buffer.Random(4).ToHexString();
      try
      {
        UserResource user = await server.Users.Create(username, cancellationToken);
        long CurrentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (Time != CurrentTime)
        {
          Console.Write($"New ID: {user.ID} Random Hits: {RandomHits}\r");

          Time = CurrentTime;
        }
      }
      catch (Exception exception)
      {
        if (exception.Message.Contains("constraint failed: User.Username"))
        {
          lock (server)
          {
            RandomHits++;
          }
          // Console.Write($"Already exists: {id}\r");
        }
        else

        {
          Console.Error.WriteLine(exception);
        }

      }

      // await Task.Delay(100);
    }
  }

  public static async Task Main(string[] args)
  {
    EnderBytesServer server = new();

    CancellationTokenSource source = new();
    RegisterCancelKey(source);

    CancellationToken cancellationToken = source.Token;
    try
    {
      await server.Init(source.Token);

      server.Logger.Logged += (level, scope, message, time) =>
      {
        Console.WriteLine($"[{time}][{scope}][{level}] {message}");
      };

      await Task.WhenAll([
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken)),
        Task.Run(async () => await A(server, cancellationToken))
      ]);

      // await server.Listen(source.Token);
    }
    catch (Exception exception)
    {
      Console.Error.WriteLine(exception);
    }
    finally
    {
      source.Dispose();
    }
  }
}

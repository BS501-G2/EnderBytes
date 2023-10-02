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
        string levelString = level switch
        {
          EnderBytesLogger.LOGLEVEL_VERBOSE => "Verbose",
          EnderBytesLogger.LOGLEVEL_INFO => "Info",
          EnderBytesLogger.LOGLEVEL_WARN => "Warning",
          EnderBytesLogger.LOGLEVEL_ERROR => "Error",
          EnderBytesLogger.LOGLEVEL_FATAL => "Fatal",

          _ => "Unknown"
        };

        Console.WriteLine($"[{time}][{scope}][{levelString}] {message}");
      };

      while (true)
      {
        await server.RunTransaction(async (connection, cancellationToken) =>
        {
          for (int index = 0 ; index < 1; index++)
          {
            UserResource user = await server.Users.Create(connection, Buffer.Random(8).ToHexString(), cancellationToken);
            await server.UserAuthentications.CreatePassword(connection, user, "dasdaASsd(^&921)", cancellationToken);
            await server.Guilds.Create(connection, user, "ASDLKAMSLDMK", null, cancellationToken);
          }
        }, cancellationToken);
      }
    }
    finally
    {
      source.Dispose();
    }
  }
}

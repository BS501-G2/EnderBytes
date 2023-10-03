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
      await server.Resources.Init(source.Token);

      server.Logger.Logged += (level, scope, message, time) =>
      {
        string levelString = level switch
        {
          Logger.LOGLEVEL_VERBOSE => "Verbose",
          Logger.LOGLEVEL_INFO => "Info",
          Logger.LOGLEVEL_WARN => "Warning",
          Logger.LOGLEVEL_ERROR => "Error",
          Logger.LOGLEVEL_FATAL => "Fatal",

          _ => "Unknown"
        };

        // if (level >= EnderBytesLogger.LOGLEVEL_VERBOSE)
        // {
        //   return;
        // }

        Console.WriteLine($"[{time}][{levelString}][{scope}] {message}");
      };

      while (true)
      {
        try
        {
          await server.RunTransaction(async (connection, cancellationToken) =>
          {
            for (int index = 0; index < 1000; index++)
            {
              UserResource user = await server.Users.Create(connection, Buffer.Random(8).ToHexString(), cancellationToken);
              await server.UserAuthentications.CreatePassword(connection, user, Random.Shared.Next(10) == 1 ? "asd" : "dasdaASsd(^&921)", cancellationToken);
              await server.Guilds.Create(connection, user, "ASDLKAMSLDMK", null, cancellationToken);
            }
          }, cancellationToken);
        }
        catch (Exception exception)
        {
          Console.Error.WriteLine(exception);
        }
      }
    }
    finally
    {
      source.Dispose();
    }
  }
}

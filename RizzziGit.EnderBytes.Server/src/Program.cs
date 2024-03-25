using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Commons.Logging;

using Core;
using Utilities;
using Services;

public static class Program
{
  public static Server Server = new(new(
      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "10.0.0.3",
        Database = "enderbytes",

        UserID = "enderbytes",
        Password = "enderbytes",

        AllowBatch = true
      },

      KeyGeneratorThreads: 8
    ));

  public static async Task Main()
  {
    {
      StringBuilder buffer = new();
      Server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"-> [{DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).ToLocalTime()} / {level.ToString().ToUpper()}] [{scope}] {message}");

      ConsoleCancelEventHandler? onCancel = null;
      onCancel = (_, _) =>
      {
        Console.CancelKeyPress -= onCancel!;
        Console.CancelKeyPress += (_, _) => Environment.Exit(0);

        Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
        StopTest(Server).WaitSync();
      };

      Console.CancelKeyPress += onCancel!;
    }

    Logger testLogger = new("Test");
    Server.Logger.Subscribe(testLogger);

    await Server.Start();
    try
    {
      try
      {
        await RunTest(testLogger, Server);
      }
      catch
      {
        Handler = null;
        throw;
      }
      await Server.Join();
    }
    catch (Exception exception)
    {
      try
      {
        await StopTest(Server);
      }
      catch (Exception stopException)
      {
        throw new AggregateException(exception, stopException);
      }

      throw;
    }
  }

  private static event ResourceService.TransactionHandler? Handler = null;

  public static async Task StopTest(Server server) => await Task.Run(async () =>
  {
    if (Handler != null)
    {
      await server.ResourceService.Transact(Handler);
    }

    await server.Stop();
  });

  public static Task RunTest(Logger logger, Server server) => Task.Run(() =>
  {
    return Task.CompletedTask;
  });
}

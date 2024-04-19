using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Commons.Logging;

using Core;
using Utilities;
using Services;
using RizzziGit.EnderBytes.Resources;

public static class Program
{
  public static Server Server = new(new(
      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "localhost",
        Database = "enderbytes",

        UserID = "test",
        Password = "test",

        AllowBatch = true,
      },

      HttpClientPort: 8083,

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
        Handlers.Clear();
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

  private readonly static List<ResourceService.TransactionHandler> Handlers = [];

  public static async Task StopTest(Server server) => await Task.Run(async () =>
  {
    foreach (ResourceService.TransactionHandler handler in Handlers)
    {
      await server.ResourceService.Transact(handler);
    }

    await server.Stop();
  });

  public static Task RunTest(Logger logger, Server server) => Task.Run(() =>
  {
    server.ResourceService.Transact(async (transaction, cancellationToken) =>
    {
      (UserManager.Resource user, UserAuthenticationToken userAuthenticationToken) = await server.ResourceService.GetManager<UserManager>().Create(transaction, "Testuser", "LastName", "FirstName", "MiddleName", "TestTest123;",  cancellationToken);
      Handlers.Add(async (transaction, cancellationToken) =>
      {
        await server.ResourceService.GetManager<UserManager>().Delete(transaction, user, cancellationToken);
      });
    });
  });
}

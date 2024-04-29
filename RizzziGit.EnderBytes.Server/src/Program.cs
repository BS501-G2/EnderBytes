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
  public static readonly Server Server = new(new(
      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "10.0.0.3",
        Database = "enderbytes",

        UserID = "enderbytes",
        Password = "enderbytes",

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

      static void onCancel(object? _, ConsoleCancelEventArgs s)
      {
        Console.CancelKeyPress -= onCancel;
        Console.CancelKeyPress += (_, _) => Environment.Exit(0);

        Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
        StopTest(Server).WaitSync();
      }

      Console.CancelKeyPress += onCancel;
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

  public static Task RunTest(Logger logger, Server server) => Task.Run(async () =>
  {
    await server.ResourceService.Transact(async (transaction) =>
    {
      UserManager userManager = transaction.GetManager<UserManager>();
      FileManager fileManager = transaction.GetManager<FileManager>();
      FileContentManager fileContentManager = transaction.GetManager<FileContentManager>();
      FileContentDataManager fileContentDataManager = transaction.GetManager<FileContentDataManager>();

      (UserManager.Resource user, UserAuthenticationToken userAuthentication) = await userManager.Create(transaction, "testuser", "User", "Test", "Middle", "TestTest123;");

      FileManager.Resource root = await fileManager.GetRootFromUser(transaction, userAuthentication);
      FileManager.Resource file = await fileManager.Create(transaction, root, "Test", false, userAuthentication);

      FileContentManager.Resource fileContent = await fileContentManager.GetRootFileMetadata(transaction, file);

      KeyService.AesPair key = await fileManager.GetKey(transaction, file, FileAccessExtent.ReadWrite, userAuthentication);

      await fileContentDataManager.Write(transaction, file, key, fileContent, Encoding.UTF8.GetBytes("Hello World!"));

      Console.WriteLine((await fileContentDataManager.Read(transaction, file, fileContent)).ToString());
    });
  });
}

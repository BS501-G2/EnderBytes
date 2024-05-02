using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Commons.Logging;

using Core;
using Utilities;
using Services;
using RizzziGit.EnderBytes.Resources;
using RizzziGit.Commons.Memory;
using Microsoft.Win32.SafeHandles;

public static class Program
{
  public static readonly Server Server = new(new(
      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "10.3",
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
      FileContentVersionManager fileContentVersionManager = transaction.GetManager<FileContentVersionManager>();
      FileDataManager fileDataManager = transaction.GetManager<FileDataManager>();

      (UserManager.Resource user, UserAuthenticationToken userAuthenticationToken) = await userManager.Create(transaction, "testuser", "Rection", "Hugh", "G", "TestTest123;");
      Handlers.Add(async (transaction) => await userManager.Delete(transaction, user));

      FileManager.Resource rootFolder = await fileManager.GetRootFromUser(transaction, userAuthenticationToken);

      FileManager.Resource testFile = await fileManager.Create(transaction, rootFolder, "test.txt", false, userAuthenticationToken);

      FileContentManager.Resource testFileContent = await fileContentManager.GetMainContent(transaction, testFile);

      FileContentVersionManager.Resource[] testFileVersions = await fileContentVersionManager.List(transaction, testFileContent);

      FileContentVersionManager.Resource testFileVersion = testFileVersions[0];

      KeyService.AesPair testFileKey = await fileManager.GetKeyRequired(transaction, testFile, FileAccessExtent.Full, userAuthenticationToken);

      CompositeBuffer bytes = "Hello, World!";

      await fileDataManager.Write(transaction, testFile, testFileKey, testFileContent, testFileVersion, bytes);
      await fileDataManager.Write(transaction, testFile, testFileKey, testFileContent, testFileVersion, bytes, 1024 * 1024);

      Console.WriteLine(await fileDataManager.Read(transaction, testFile, testFileKey, testFileContent, testFileVersion, 0, bytes.Length + 1024 * 1024));
    });
  });
}

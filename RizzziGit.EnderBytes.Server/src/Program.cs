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
        Server = "10.1.0.117",
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
      FileAccessManager fileAccessManager = transaction.GetManager<FileAccessManager>();

      (UserManager.Resource user, UserAuthenticationToken userAuthenticationToken) = await userManager.Create(transaction, "testuser", "Rection", "Hugh", "G", "TestTest123;");
      Handlers.Add(async (transaction) => await userManager.Delete(transaction, user));

      (UserManager.Resource otherUser, UserAuthenticationToken otherUserAuthenticationToken) = await userManager.Create(transaction, "testuser2", "Rection", "Hugh", "G", "TestTest123;");
      Handlers.Add(async (transaction) => await userManager.Delete(transaction, otherUser));

      FileManager.Resource rootFolder = await fileManager.GetRootFromUser(transaction, userAuthenticationToken);

      (FileManager.Resource readWriteFolder, KeyService.AesPair readWriteFolderKey) = await fileManager.Create(transaction, rootFolder, "Read-Write Folder", true, userAuthenticationToken);
      (FileManager.Resource readOnlyFolder, KeyService.AesPair readOnlyFolderKey) = await fileManager.Create(transaction, rootFolder, "Read-Only Folder", true, userAuthenticationToken);

      await fileAccessManager.GrantUser(transaction, readWriteFolder, otherUser, readOnlyFolderKey, FileAccessExtent.ReadWrite);
      await fileAccessManager.GrantUser(transaction, readOnlyFolder, otherUser, readOnlyFolderKey, FileAccessExtent.ReadOnly);
    });
  });
}

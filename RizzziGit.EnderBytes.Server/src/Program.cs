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
        Server = "25.20.99.238",
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

  public static Task RunTest(Logger logger, Server server) => Task.Run(async () =>
  {
    var (storage, folder, userAuthenticationToken) = await server.ResourceService.Transact(async (transaction, cancellationToken) =>
    {
      UserManager userManager = transaction.GetManager<UserManager>();
      StorageManager storageManager = transaction.GetManager<StorageManager>();
      FileManager fileManager = transaction.GetManager<FileManager>();

      (UserManager.Resource user, UserAuthenticationToken userAuthenticationToken) = await userManager.Create(transaction, "Testuser", "LastName", "FirstName", "MiddleName", "TestTest123;", cancellationToken);
      Handlers.Add(async (transaction, cancellationToken) =>
      {
        await server.ResourceService.GetManager<UserManager>().Delete(transaction, user, cancellationToken);
      });

      (UserManager.Resource otherUser, UserAuthenticationToken otherUserAuthenticationToken) = await userManager.Create(transaction, "Testuser2", "LastName", "FirstName", "MiddleName", "TestTest123;", cancellationToken);
      Handlers.Add(async (transaction, cancellationToken) =>
      {
        await server.ResourceService.GetManager<UserManager>().Delete(transaction, otherUser, cancellationToken);
      });

      StorageManager.Resource storage = await storageManager.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
      FileManager.Resource rootFolder = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);
      FileManager.Resource testFolder = await fileManager.CreateFolder(transaction, storage, rootFolder, "Test Folder", userAuthenticationToken, cancellationToken);
      FileAccessManager.Resource fileAccess = await server.ResourceService.GetManager<FileAccessManager>().Create(transaction, storage, testFolder, otherUser, FileAccessType.Read, userAuthenticationToken, cancellationToken);

      return (
        storage,
        testFolder,
        userAuthenticationToken
      );
    });

    List<Task> tasks = [];

    for (long i = 0; i < 100; i++)
    {
      long capturedI = i;
      tasks.Add(server.ResourceService.Transact(async (transaction, cancellationToken) =>
      {
        await server.ResourceService.GetManager<FileManager>().CreateFolder(transaction, storage, folder, $"Test Folder {capturedI}", userAuthenticationToken, cancellationToken);
      }));
    }

    await Task.WhenAll(tasks);
  });
}

using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Framework.Logging;
using Framework.Memory;

using Core;
using Resources;
using Utilities;
using Services;

public static class Program
{
  public static async Task Main()
  {
    Server server = new(new(
      KeyGeneratorThreads: 8,

      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "10.1.0.117",
        Database = "enderbytes",

        UserID = "test",
        Password = "test",

        AllowBatch = true
      }
    ));
    {
      StringBuilder buffer = new();
      server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"-> [{timestamp} / {level}] [{scope}] {message}");

      ConsoleCancelEventHandler? onCancel = null;
      onCancel = (_, _) =>
      {
        Console.CancelKeyPress -= onCancel!;
        Console.CancelKeyPress += (_, _) => Environment.Exit(0);

        Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
        StopTest(server).WaitSync();
      };

      Console.CancelKeyPress += onCancel!;
    }

    Logger testLogger = new("Test");
    server.Logger.Subscribe(testLogger);

    await server.Start();
    try
    {
      try
      {
        await RunTest(testLogger, server);
      }
      catch
      {
        Handler = null;
        throw;
      }
      await server.Join();
    }
    catch (Exception exception)
    {
      try
      {
        await StopTest(server);
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

  public static async Task RunTest(Logger logger, Server server) => await Task.Run(async () =>
  {
    await server.ResourceService.Transact((transaction, cancellationToken) =>
    {
      ResourceService service = transaction.ResoruceService;

      (UserResource originalUser, UserAuthenticationResource.UserAuthenticationToken originalToken) = service.Users.Create(transaction, "testuser", "Test User", "test", cancellationToken);
      Handler += (transaction, cancellationToken) => service.Users.Delete(transaction, originalUser, cancellationToken);

      (UserResource otherUser, UserAuthenticationResource.UserAuthenticationToken otherToken) = service.Users.Create(transaction, "testuser2", "Other User", "test", cancellationToken);
      Handler += (transaction, cancellationToken) => service.Users.Delete(transaction, otherUser, cancellationToken);

      StorageResource storage = service.Storages.Create(transaction, originalUser, originalToken, cancellationToken);

      FileResource file = service.Files.Create(transaction, storage, null, FileResource.FileType.File, "Test", originalToken, cancellationToken);
      Console.WriteLine($"File Key: {CompositeBuffer.From(service.Storages.DecryptFileKey(transaction, storage, file, originalToken, cancellationToken).Serialize()).ToHexString()}");

      FileResource? lastFolder = null;
      for (int index = 0; index < 10; index++)
      {
        FileResource folder = service.Files.Create(transaction, storage, null, FileResource.FileType.Folder, "Folder2", originalToken, cancellationToken);

        if (lastFolder != null)
        {
          service.Files.Move(transaction, storage, lastFolder, folder, originalToken, cancellationToken);
        }

        service.Files.Move(transaction, storage, file, folder, originalToken, cancellationToken);

        lastFolder = folder;
      }

      Console.WriteLine($"File Key: {CompositeBuffer.From(service.Storages.DecryptFileKey(transaction, storage, file, originalToken, cancellationToken).Serialize()).ToHexString()}");
    });
  });
}

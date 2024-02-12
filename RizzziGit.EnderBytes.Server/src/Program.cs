using System.Text;

namespace RizzziGit.EnderBytes;

using Framework.Logging;

using Core;
using Resources;
using Utilities;
using Services;
using Connections;
using Extras;

public static class Program
{
  public static async Task Main()
  {
    Server server = new(new(
      KeyGeneratorThreads: 8
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
      await RunTest(testLogger, server);
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
    await server.ResourceService.Transact((transaction, service, cancellationToken) =>
    {
      UserResource originalUser = service.Users.Create(transaction, "testuser", "Test User");
      Handler += (transaction, service, cancellationToken) => service.Users.Delete(transaction, originalUser, cancellationToken);

      UserResource otherUser = service.Users.Create(transaction, "testuser2", "Other User");
      Handler += (transaction, service, cancellationToken) => service.Users.Delete(transaction, otherUser, cancellationToken);

      UserAuthenticationResource.Token originalToken = service.UserAuthentications.CreatePassword(transaction, originalUser, "TestPass19@");
      UserAuthenticationResource.Token otherToken = service.UserAuthentications.CreatePassword(transaction, otherUser, "TestPass19@");

      FileHubResource fileHub = service.FileHubs.GetPersonal(transaction, originalToken);
      Handler += (transaction, service, cancellationToken) => service.FileHubs.Delete(transaction, fileHub, cancellationToken);

      FileResource root = service.Files.GetRootFolder(transaction, fileHub, originalToken, cancellationToken);

      FileResource folder1 = service.Files.Create(transaction, fileHub, root, FileResource.FileNodeType.Folder, "test1", originalToken, cancellationToken);
      FileResource folder2 = service.Files.Create(transaction, fileHub, root, FileResource.FileNodeType.Folder, "test2", originalToken, cancellationToken);

      service.FileAccesses.Create(transaction, fileHub, folder1, originalToken, otherToken.UserAuthentication, FileAccessResource.AccessType.Read);
      service.FileAccesses.Create(transaction, fileHub, folder2, originalToken, otherToken.UserAuthentication, FileAccessResource.AccessType.Read);

      FileResource file = service.Files.Create(transaction, fileHub, folder1, FileResource.FileNodeType.File, "file", originalToken, cancellationToken);

      logger.Info($"Folder 1 Files: {string.Join(", ", service.Files.Scan(transaction, fileHub, folder1, otherToken, cancellationToken: cancellationToken).Select((e) => e.Name))}");

      service.Files.Move(transaction, fileHub, file, folder2, otherToken, cancellationToken);
      logger.Info($"Folder 2 Files: {string.Join(", ", service.Files.Scan(transaction, fileHub, folder2, otherToken, cancellationToken: cancellationToken).Select((e) => e.Name))}");
    });
  });
}

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

  private static UserResource? User;

  public static async Task StopTest(Server server) => await Task.Run(async () =>
  {
    UserResource.ResourceManager users = server.ResourceService.Users;

    await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, _) =>
    {
      users.Delete(transaction, User!);
    });

    await server.Stop();
  });

  public static async Task RunTest(Logger logger, Server server) => await Task.Run(async () =>
  {
    UserResource.ResourceManager users = server.ResourceService.Users;
    UserAuthenticationResource.ResourceManager userAuthentications = server.ResourceService.UserAuthentications;
    FileHubResource.ResourceManager hubs = server.ResourceService.FileHubs;

    (UserAuthenticationResource.Token token, FileHubResource hubResource) = await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, _) =>
    {
      UserResource user = User = users.Create(transaction, "testuser", "Test User");
      UserAuthenticationResource.Token token = userAuthentications.CreatePassword(transaction, user, "TestPass19@");
      FileHubResource hub = hubs.GetPersonal(transaction, token);

      return (token, hub);
    });

    FileService.Hub hub = server.FileService.Get(hubResource);
    logger.Info("Got FileService.Hub.");

    ConnectionService.Connection connection = server.ConnectionService.NewConnection(new AdvancedConnection.ConnectionConfiguration(new ConnectionEndPoint.Null(), new ConnectionEndPoint.Null()));
    logger.Info("Got new Connection.");

    connection.Authenticate(token);
    logger.Info("Connection Authenticated.");

    FileService.Hub.Context hubContext = hub.GetContext(connection);
    logger.Info("Got FileService.Hub.Context.");
  });
}

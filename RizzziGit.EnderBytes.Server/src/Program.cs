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
    await server.ResourceService.Transact((transaction, service, _) => service.Users.Delete(transaction, User!));
    await server.Stop();
  });

  public static async Task RunTest(Logger logger, Server server) => await Task.Run(async () =>
  {
    UserAuthenticationResource.Token token = await server.ResourceService.Transact((transaction, service, _) =>
    {
      UserResource user = User = service.Users.Create(transaction, "testuser", "Test User");
      UserAuthenticationResource.Token token = service.UserAuthentications.CreatePassword(transaction, user, "TestPass19@");

      return token;
    });

    FileHubResource hubResource = await server.ResourceService.Transact((transaction, service, _) => service.FileHubs.GetPersonal(transaction, token));

    FileService.Hub hub = server.FileService.Get(hubResource);
    logger.Info("Got FileService.Hub.");

    AdvancedConnection connection = server.ConnectionService.NewConnection(new AdvancedConnection.ConnectionConfiguration(new ConnectionEndPoint.Null(), new ConnectionEndPoint.Null()));
    logger.Info("Got new Connection.");

    ConnectionService.Response response = await connection.ExecuteRequest(new ConnectionService.Request.Login("testuser", UserAuthenticationResource.UserAuthenticationType.Password, Encoding.UTF8.GetBytes("TestPass19@")));
    logger.Info("Connection Authenticated.");
  });
}

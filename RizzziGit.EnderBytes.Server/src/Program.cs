using MongoDB.Driver;

namespace RizzziGit.EnderBytes;

using System.Text;
using Core;
using Resources;
using Services;
using Utilities;

public static class Program
{
  public static async Task Main()
  {
    Server server = new(new(null, new()
    {
      Server = new MongoServerAddress("10.1.0.128")
    }, null));

    server.Logger.Logged += (level, scope, message, timestamp) => Console.WriteLine($"[{timestamp} / {level}] [{scope}] {message}");

    Console.CancelKeyPress += (_, _) =>
    {
      server.Stop().Wait();
    };

    await server.Start();

    (User user, UserAuthentication userAuthentication) = server.ResourceService.Users.Create("test", "test", UserAuthenticationType.Password, Encoding.Default.GetBytes("test"));
    ConnectionService.Connection connection = server.ConnectionService.NewConnection(new ConnectionService.Parameters.Internal(userAuthentication, userAuthentication.GetPayloadHash(Encoding.Default.GetBytes("test"))));
    StorageService.Storage.Session storage = connection.Storage;

    await server.Join();
  }
}

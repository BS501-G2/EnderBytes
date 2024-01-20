using MongoDB.Driver;
using RizzziGit.Framework.Logging;

namespace RizzziGit.EnderBytes;

public static class Program
{
  public static async Task Main()
  {
    Server server = new(new(null, new()
    {
      Server = new MongoServerAddress("10.1.0.128")
    }, null));

    server.Logger.Logged += (level, scope, message, timestamp) => Console.WriteLine($"[{timestamp} / {level}][{scope}] {message}");

    await server.Start();
    await server.Join();
  }
}

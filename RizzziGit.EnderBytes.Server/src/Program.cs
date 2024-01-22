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
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"[{timestamp} / {level}] [{scope}] {message}");

    Console.CancelKeyPress += (_, _) =>
    {
      Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
      server.Stop().Wait();
    };

    await server.Start();
    await server.Join();
  }
}

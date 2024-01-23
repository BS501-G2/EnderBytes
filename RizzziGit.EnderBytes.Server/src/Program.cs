namespace RizzziGit.EnderBytes;

using System.Security.Cryptography;
using System.Text;
using Core;
using Resources;
using RizzziGit.Framework.Memory;
using Services;
using Utilities;

public static class Program
{
  public static async Task Main()
  {
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"-> [{timestamp} / {level}] [{scope}] {message}");

    Console.CancelKeyPress += (_, _) =>
    {
      Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
      server.Stop().Wait();
    };

    await server.Start();
    try
    {
      UserResource.ResourceManager users = server.ResourceService.Users;

      await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction) =>
      {
        static string randomHex () => ((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString();

        UserResource user = users.Create(transaction, randomHex(), randomHex());
        Console.WriteLine(users.Update(transaction, user, randomHex(), randomHex()));

        Console.WriteLine($"User: {user.Username}, Display: {user.DisplayName}");
      });

      await server.Join();
    }
    catch (Exception exception)
    {
      try
      {
        await server.Stop();
      }
      catch (Exception stopException)
      {
        throw new AggregateException(exception, stopException);
      }

      throw;
    }
  }
}

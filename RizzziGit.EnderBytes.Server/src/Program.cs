using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes;

using Framework.Memory;
using Framework.Services;

using Core;
using Resources;
using Services;

public static class Program
{
  public static async Task Main()
  {
    Server server = new();

    {
      StringBuilder buffer = new();
      server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"-> [{timestamp} / {level}] [{scope}] {message}");

      Console.CancelKeyPress += (_, _) =>
      {
        Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
        server.Stop().Wait();
      };
    }

    await server.Start();
    try
    {
      UserResource.ResourceManager users = server.ResourceService.Users;

      List<UserResource> a = [];

      long lastTime = 0;
      await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction) =>
      {
        for (int iteration = 0; iteration < 1000000; iteration++)
        {
          static string randomHex() => ((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString();

          UserResource user = users.Create(transaction, randomHex(), randomHex());
          users.Update(transaction, user, randomHex(), randomHex());
          // users.Delete(transaction, user);

          a.Add(user);

          if (lastTime != (lastTime = DateTimeOffset.Now.ToUnixTimeSeconds()))
          {
            Console.WriteLine(user.Id);
          }
        }
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

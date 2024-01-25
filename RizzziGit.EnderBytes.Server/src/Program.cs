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

    try
    {
      await server.Start();
      try
      {
        await Task.Run(async () =>
        {
          UserResource.ResourceManager users = server.ResourceService.Users;
          UserAuthenticationResource.ResourceManager userAuthentications = server.ResourceService.UserAuthentications;

          CancellationTokenSource source = new();
          for (int iteration = 0; iteration < 1000; iteration++)
          {
            await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, cancellationToken) =>
            {
              cancellationToken.ThrowIfCancellationRequested();
              static string randomHex() => ((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString();

              UserResource user = users.Create(transaction, randomHex(), randomHex());
              Console.WriteLine(user.Id);
              users.Update(transaction, user, "test", randomHex());

              (UserAuthenticationResource userAuthentication, byte[] payloadHash) = userAuthentications.CreatePassword(transaction, user, "test");
              (UserAuthenticationResource userAuthentication2, byte[] payloadHash2) = userAuthentications.CreatePassword(transaction, user, userAuthentication, RandomNumberGenerator.GetBytes(32), randomHex());
              // users.Delete(transaction, user);
            }, source.Token);
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
    catch { }
  }
}

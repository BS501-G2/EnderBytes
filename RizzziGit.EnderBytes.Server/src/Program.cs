using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes;

using Framework.Memory;

using Core;
using Resources;
using Services;

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
        // await RunTest(server);
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

  public static async Task RunTest(Server server) => await Task.Run(async () =>
  {
    UserResource.ResourceManager users = server.ResourceService.Users;
    UserAuthenticationResource.ResourceManager userAuthentications = server.ResourceService.UserAuthentications;
    await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, cancellationToken) =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      static string randomHex() => ((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString();

      UserResource user = users.Create(transaction, randomHex(), randomHex());

      (UserAuthenticationResource userAuthentication1, byte[] payloadHash1) = userAuthentications.CreatePassword(transaction, user, "test");

      Console.WriteLine();
      Console.WriteLine($"User: @{user.Username} (#{user.Id})");
      Console.WriteLine($"Aser Authentications:");
      foreach (UserAuthenticationResource userAuthentication in userAuthentications.List(transaction, user))
      {
        Console.WriteLine($" -> {userAuthentication.Type} #{userAuthentication.Id}");

        byte[] payload = Encoding.UTF8.GetBytes("test");
        byte[] payloadHash = userAuthentication.GetPayloadHash(payload);

        byte[] test = RandomNumberGenerator.GetBytes(32);
        byte[] testEncrypted = userAuthentication.Encrypt(test);

        Console.WriteLine($"    -> Encryption and Decryption works properly: {userAuthentication.Decrypt(testEncrypted, payloadHash).SequenceEqual(test)}.");
      }
      Console.WriteLine();
      users.Delete(transaction, user);
    });
  });
}

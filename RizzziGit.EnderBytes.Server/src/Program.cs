using System.Text;

namespace RizzziGit.EnderBytes;

using Core;
using Resources;
using Utilities;
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
        StopTest(server).WaitSync();
      };
    }

    await server.Start();
    try
    {
      await RunTest(server);
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

  public static async Task RunTest(Server server) => await Task.Run(async () =>
  {
    UserResource.ResourceManager users = server.ResourceService.Users;
    UserAuthenticationResource.ResourceManager userAuthentications = server.ResourceService.UserAuthentications;
    FileHubResource.ResourceManager fileHubs = server.ResourceService.FileHubs;

    await server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, _) =>
    {
      UserResource user = User = users.Create(transaction, "testuser", "Test User");
      UserAuthenticationResource.Pair pair = userAuthentications.CreatePassword(transaction, user, "TestPass19@");
      FileHubResource fileHub = fileHubs.GetPersonal(transaction, user, pair.UserAuthentication);

      // cancellationToken.ThrowIfCancellationRequested();
      // static string randomHex() => ((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString();

      // UserResource user = users.Create(transaction, randomHex(), randomHex());

      // UserAuthenticationResource.Pair pair = userAuthentications.CreatePassword(transaction, user, "test");
      // userAuthentications.Delete(transaction, pair.UserAuthentication);
      // for (int index = 0; index < 10; index++)
      // {
      //   pair = userAuthentications.CreatePassword(transaction, user, pair, "test");
      // }

      // Console.WriteLine();
      // Console.WriteLine($"User: @{user.Username} (#{user.Id})");
      // Console.WriteLine($"Aser Authentications:");
      // foreach (UserAuthenticationResource userAuthentication in userAuthentications.List(transaction, user))
      // {
      //   Console.WriteLine($" -> {userAuthentication.Type} #{userAuthentication.Id}");

      //   byte[] payload = Encoding.UTF8.GetBytes("test");
      //   byte[] payloadHash = userAuthentication.GetPayloadHash(payload);

      //   byte[] test = RandomNumberGenerator.GetBytes(32);
      //   byte[] testEncrypted = userAuthentication.Encrypt(test);

      //   Console.WriteLine($"    -> Encryption and Decryption works properly: {userAuthentication.Decrypt(testEncrypted, payloadHash).SequenceEqual(test)}.");
      // }
      // Console.WriteLine();
      // users.Delete(transaction, user);
    });
  });
}

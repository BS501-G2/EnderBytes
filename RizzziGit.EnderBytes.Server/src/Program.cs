namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Database;
public static class Program
{
  public static void RegisterCancelEvent(Server server)
  {
    void onPress(object? sender, EventArgs args)
    {
      Console.CancelKeyPress -= onPress;
      server.Stop().Wait();
    }
    Console.CancelKeyPress += onPress;
  }

  public static async Task Test(Server server)
  {
    for (int count = 0; count < 1 && server.State == ServiceState.Started; count++)
    {
      await server.Resources.Database.RunTransaction((transaction) =>
      {
        string username = $"te{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        string password = "aasdAAASD1123123;";

        UserResource user = server.Resources.Users.Create(transaction, username, "Test user");
        var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user, password);
        var (privateKey, publicKey) = server.KeyGenerator.GetNew();
        UserKeyResource userKey = server.Resources.UserKeys.Create(transaction, user, userAuthentication, privateKey, publicKey, hashCache);

        // StoragePool storagePool = server.Resources.StoragePools.CreateBlob(transaction, userKey.Id, user.Id, StoragePoolFlags.IgnoreCase);
      }, CancellationToken.None);
    }
  }

  public static async Task Main(string[] args)
  {
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) =>
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
      // if (level >= LogLevel.Verbose)
      // {
      //   return;
      // }
      Console.WriteLine($"[{dateTimeOffset} {Enum.GetName(level)?.ToUpper()}] [{scope}] {message}");
    };

    try
    {
      await server.Start();
      RegisterCancelEvent(server);
      await Test(server);

      await server.Join();
    }
    catch
    {
      await server.Stop();

      throw;
    }
  }
}

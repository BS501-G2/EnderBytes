namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Connections;

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

  public static async void Test(Server server)
  {
    try
    {
      long index = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      string username = $"abcdefg";
      string password = $"Aa11111;";
      UserResource user = await server.Resources.MainDatabase.RunTransaction((transaction) =>
      {
        UserResource user = server.Resources.Users.Create(transaction, username, "Test User");
        var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, password);

        return user;
      }, CancellationToken.None);
    }
    catch (Exception exception)
    {
      Console.WriteLine(exception);
      // await server.Stop();
    }
  }

  public static async Task Main(string[] _)
  {
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) =>
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
      // if ((int)level >= (int)LogLevel.Verbose)
      // {
      //   return;
      // }
      Console.WriteLine($"[{dateTimeOffset} {Enum.GetName(level)?.ToUpper()}] [{scope}] {message}");
    };

    await server.Start();
    RegisterCancelEvent(server);
    Test(server);

    await Task.Delay(1000);
    await server.Join();
  }
}

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
    Connection connection = await server.Connections.GetClientConnection(CancellationToken.None);

    try
    {
      while (server.State == ServiceState.Started)
      {
        long index = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string username = $"A{index}";
        string password = $"aA32323{index};";
        UserResource user = await server.Resources.MainDatabase.RunTransaction((transaction) =>
        {
          UserResource user = server.Resources.Users.Create(transaction, username, "Test User");
          var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, password);

          return user;
        }, CancellationToken.None);
        Console.WriteLine(await connection.Execute(new Connection.Request.Login(username, password)));
        Console.WriteLine(await connection.Execute(new Connection.Request.WhoAmI()));
        await server.Resources.MainDatabase.RunTransaction(async (transaction, cancellationToken) =>
        {
          await user.Manager.Delete(transaction, user, cancellationToken);
        }, CancellationToken.None);
        Console.WriteLine(await connection.Execute(new Connection.Request.Logout()));
      }
    }
    catch (Exception exception)
    {
      Console.WriteLine(exception);
      connection.Close();
      await server.Stop();
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
    // Test(server);

    await Task.Delay(1000);
    await server.Join();
  }
}

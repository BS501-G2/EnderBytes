namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Connections;

public static class Program
{
  public static void RegisterCancelEvent(Server server)
  {
    bool cancelled = false;
    void onPress(object? sender, EventArgs args)
    {
      if (!cancelled)
      {
        Task _ = server.Stop();
        cancelled = true;
      }
      else
      {
        Console.CancelKeyPress -= onPress;
      }
    }
    Console.CancelKeyPress += onPress;
  }

  public static async Task Main(string[] _)
  {
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) =>
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
      Console.WriteLine($"[{dateTimeOffset} {Enum.GetName(level)?.ToUpper()}] [{scope}] {message}");
    };

    await server.Start();
    RegisterCancelEvent(server);

    Connection connection = await server.Connections.GetClientConnection(CancellationToken.None);
    UserResource user = await server.Resources.MainDatabase.RunTransaction((transaction) => {
      UserResource user = server.Resources.Users.Create(transaction, "ASDADADA", "Test User");
      var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, "ASDKLO)_()la3");

      return user;
    }, CancellationToken.None);
    await connection.Execute(new Connection.Request.Login("asd", "asd"));
    await server.Resources.MainDatabase.RunTransaction(async (transaction, cancellationToken) => {
      await user.Manager.Delete(transaction, user, cancellationToken);
    }, CancellationToken.None);
    await Task.Delay(1000);
    connection.Close();
    await server.Join();
  }
}

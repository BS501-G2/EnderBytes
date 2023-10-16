namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Connections;

public static class Program
{
  public static void RegisterCancelEvent(Server server)
  {
    void onPress(object? sender, EventArgs args)
    {
      _ = server.Stop();
      Console.CancelKeyPress -= onPress;
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

    for (int index = 0; index < 10000; index++)
    {
      Connection connection = await server.Connections.GetClientConnection(CancellationToken.None);
      UserResource user = await server.Resources.MainDatabase.RunTransaction((transaction) =>
      {
        UserResource user = server.Resources.Users.Create(transaction, "ASDADADA", "Test User");
        var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, "ASDKLO)_()la3");

        return user;
      }, CancellationToken.None);
      Connection.Response response = await connection.Execute(new Connection.Request.Login("ASDADADA", "ASDKLO)_()la3"));
      Console.WriteLine(response);
      await server.Resources.MainDatabase.RunTransaction(async (transaction, cancellationToken) =>
      {
        await user.Manager.Delete(transaction, user, cancellationToken);
      }, CancellationToken.None);
      Console.WriteLine(await connection.Execute(new Connection.Request.Login("ASDADADA", "ASDKLO)_()la3")));
      connection.Close();
    }
    await Task.Delay(1000);
    await server.Join();
  }
}

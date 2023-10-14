namespace RizzziGit.EnderBytes.Runtime;

using Resources;

public static class Program
{
  public static async Task Main(string[] _)
  {
    Server server = new();

    server.Logger.Logged += (level, scope, message, timestamp) =>
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
      Console.WriteLine($"[{dateTimeOffset} {Enum.GetName(level)?.ToUpper()}] [{scope}] {message}");
    };

    TaskCompletionSource onReady = new();
    Task task = server.Run(onReady, CancellationToken.None);
    await onReady.Task;
    await server.Resources.MainDatabase.RunTransaction((transaction) =>
    {
      UserResource user = server.Resources.Users.Create(transaction, "Ajsdoimsdfg", "Test");
      UserAuthenticationResource userAuthentication = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, "testT@3123");
    }, CancellationToken.None);
    await task;
  }
}

namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Connections;
using RizzziGit.EnderBytes.ArtificialIntelligence;

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

  public static async Task Test(Server server, string[] args)
  {
    foreach (string arg in args)
    {
      using FileStream stream = File.OpenRead(arg);
      await foreach (TranscriptBlock transcriptBlock in server.ArtificialIntelligence.Whisper.Transcribe(stream, CancellationToken.None))
      {
        Console.WriteLine(transcriptBlock);
      }
    }

    // try
    // {
    //   string username = $"abcdefg";
    //   string password = $"Aa11111;";
    //   UserResource user = await server.Resources.MainDatabase.RunTransaction((transaction) =>
    //   {
    //     UserResource user = server.Resources.Users.Create(transaction, username, "Test User");
    //     var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, password);

    //     return user;
    //   }, CancellationToken.None);
    // }
    // catch (Exception exception)
    // {
    //   Console.WriteLine(exception);
    //   // await server.Stop();
    // }
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
      await Test(server, args);

      await server.Join();
    }
    catch
    {
      await server.Stop();

      // throw;
    }
  }
}

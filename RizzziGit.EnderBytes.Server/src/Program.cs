namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Connections;
using ArtificialIntelligence;
using StoragePools;

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
      var (storagePoolResource, userAuthentication, hashCache) = await server.Resources.MainDatabase.RunTransaction((transaction) =>
      {
        string username = $"te{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string password = "aasdAAASD1123123;";

        UserResource user = server.Resources.Users.Create(transaction, username, "Test user");
        var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user.Id, password);
        return (server.Resources.StoragePools.CreateVirtual(transaction, user.Id, StoragePoolFlags.IgnoreCase), userAuthentication, hashCache);
      }, CancellationToken.None);

      var storagePool = (BlobStoragePool)await server.StoragePools.GetStoragePool(storagePoolResource, CancellationToken.None);
      await storagePool.FileCreate(userAuthentication, hashCache, ["test.webm"], CancellationToken.None);

      // using FileStream stream = File.OpenRead(arg);
      // await foreach (TranscriptBlock transcriptBlock in server.ArtificialIntelligence.Whisper.Transcribe(stream, CancellationToken.None))
      // {
      //   Console.WriteLine(transcriptBlock);
      // }
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
      await Test(server, args);

      await server.Join();
    }
    catch
    {
      await server.Stop();

      throw;
    }
  }
}

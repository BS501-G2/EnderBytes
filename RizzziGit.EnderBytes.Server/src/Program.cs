namespace RizzziGit.EnderBytes.Runtime;

using Resources;
using Database;
using Resources.BlobStorage;
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

  public static void ScanFiles(TaskFactory factory, Resources.BlobStorage.ResourceManager resources, string path = "/", BlobFileResource? parentFolder = null)
  {
    _ = factory.StartNew(() =>
    {
      foreach (string entry in Directory.EnumerateFileSystemEntries(path).Select((entry) => entry[path.Length..]))
      {
        try
        {
          FileInfo fileInfo = new(Path.Join(path, entry));

          if (
            (!fileInfo.Attributes.HasFlag(FileAttributes.Directory)) ||
            fileInfo.LinkTarget != null ||
            entry.Length == 0
          )
          {
            continue;
          }
        }
        catch
        {
          continue;
        }

        // Console.WriteLine(Path.Join(path, entry));

        _ = resources.Database.RunTransaction((transaction) =>
        {
          BlobFileResource pool = resources.Files.CreateFolder(transaction, path == "/" ? null : parentFolder, entry[0] == '/' ? entry[1..] : entry);
          ScanFiles(factory, resources, Path.Join(path, entry), pool);
        }, CancellationToken.None);
      }
    });
  }

  public static async Task Test(Server server)
  {
    for (int count = 0; count < 1 && server.State == ServiceState.Started; count++)
    {
      var (storagePoolResource, userAuthentication, hashCache) = await server.Resources.Database.RunTransaction((transaction) =>
      {
        string username = $"te{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        string password = "aasdAAASD1123123;";

        UserResource user = server.Resources.Users.Create(transaction, username, "Test user");
        var (userAuthentication, hashCache) = server.Resources.UserAuthentications.CreatePassword(transaction, user, password);
        return (server.Resources.StoragePools.CreateBlob(transaction, user.Id, StoragePoolFlags.IgnoreCase), userAuthentication, hashCache);
      }, CancellationToken.None);

      BlobStoragePool blobStorage = (BlobStoragePool)await server.StoragePools.GetStoragePool(storagePoolResource, CancellationToken.None);
      TaskFactory factory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
      ScanFiles(factory, blobStorage.Resources);
      // var storagePool = (BlobStoragePool)await server.StoragePools.GetStoragePool(storagePoolResource, CancellationToken.None);
      // await storagePool.FileCreate(userAuthentication, hashCache, ["test.webm"], CancellationToken.None);

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

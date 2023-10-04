namespace RizzziGit.EnderBytes;

using Buffer;
using RizzziGit.EnderBytes.Resources;

public static class Program
{
  public static void RegisterCancelKey(CancellationTokenSource source)
  {
    void onCancel(object? sender, ConsoleCancelEventArgs args)
    {
      try { source.Cancel(); } catch { }

      Console.CancelKeyPress -= onCancel;
    }
    Console.CancelKeyPress += onCancel;
  }

  public static async Task Main(string[] args)
  {
    EnderBytesServer server = new();

    CancellationTokenSource source = new();
    RegisterCancelKey(source);

    CancellationToken cancellationToken = source.Token;
    try
    {
      await server.Resources.Init(source.Token);

      server.Logger.Logged += (level, scope, message, time) =>
      {
        string levelString = level switch
        {
          Logger.LOGLEVEL_VERBOSE => "Verbose",
          Logger.LOGLEVEL_INFO => "Info",
          Logger.LOGLEVEL_WARN => "Warning",
          Logger.LOGLEVEL_ERROR => "Error",
          Logger.LOGLEVEL_FATAL => "Fatal",

          _ => "Unknown"
        };

        // if (level >= EnderBytesLogger.LOGLEVEL_VERBOSE)
        // {
        //   return;
        // }

        Console.WriteLine($"[{time}][{levelString}][{scope}] {message}");
      };

      try
      {
        await server.RunTransaction(async (connection, cancellationToken) =>
        {
          UserResource user = await server.Resources.Users.Create(connection, "asdjoaskd", cancellationToken);
          StoragePoolResource virtualStoragePool = await server.Resources.StoragePools.CreateVirtualPool(connection, user, "asdjoaskd", 0, cancellationToken);
          VirtualStorageNodeResource node = await server.Resources.VirtualStorageNodes.Create(
            connection,
            virtualStoragePool,
            "ASD",
            null,
            VirtualStorageNodeResource.TYPE_FILE,
            VirtualStorageNodeResource.MODE_OTHERS_READ |
            VirtualStorageNodeResource.MODE_OTHERS_WRITE |
            VirtualStorageNodeResource.MODE_GROUP_READ |
            VirtualStorageNodeResource.MODE_GROUP_WRITE |
            VirtualStorageNodeResource.MODE_USER_READ |
            VirtualStorageNodeResource.MODE_USER_WRITE,
            user, cancellationToken);

          using FileStream inFile = File.OpenRead("/home/cool/Documents/Code/.stignore");
          for (byte[] buffer = new byte[1024 * 1024]; inFile.Position < inFile.Length;)
          {
            int bufferLength = inFile.Read(buffer);

            await server.Resources.VirtualStorageBlobs.Append(connection, node, Buffer.From(buffer, 0, bufferLength), cancellationToken);
          }

          using var outFile = File.OpenWrite("/run/media/cool/AC233/out.txt");
          await using var stream = await server.Resources.VirtualStorageBlobs.Stream(connection, node, null, cancellationToken);
          await foreach (VirtualStorageBlobResource blob in stream)
          {
            outFile.Write(blob.Read().ToByteArray());
          }
        }, cancellationToken);
      }
      catch (Exception exception)
      {
        Console.Error.WriteLine(exception);
      }
    }
    finally
    {
      source.Dispose();
    }
  }
}

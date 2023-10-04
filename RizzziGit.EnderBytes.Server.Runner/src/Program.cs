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
          UserResource user = await server.Resources.Users.Create(connection, Buffer.Random(4).ToHexString(), cancellationToken);
          StoragePoolResource virtualStoragePool = await server.Resources.StoragePools.CreateVirtualPool(connection, user, Buffer.Random(4).ToHexString(), 0, cancellationToken);
          foreach (var (inFile, outFile) in new (string inFile, string outFile)[] {
            ("/run/media/cool/AC233/test.webm", "/run/media/cool/AC233/out.webm")
            // ("/run/media/cool/AC233/out.txt", "/run/media/cool/AC233/out2.txt")
          })
          {
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
              user, cancellationToken
            );

            using FileStream inHandle = File.OpenRead(inFile);
            for (byte[] buffer = new byte[1024 * 1024]; inHandle.Position < inHandle.Length;)
            {
              int bufferLength = inHandle.Read(buffer);
              // await server.Resources.VirtualStorageBlobs.Insert(connection, node, (ulong)(inHandle.Position / 2), Buffer.From(buffer, 0, bufferLength), cancellationToken);

              VirtualStorageBlobResource blob = await server.Resources.VirtualStorageBlobs.Append(connection, node, Buffer.From(buffer, 0, bufferLength), cancellationToken);
              if (Random.Shared.Next(3) == 0)
              {
                await server.Resources.VirtualStorageBlobs.Delete(connection, blob, cancellationToken);
              }
            }


            using var outHandle = File.OpenWrite(outFile);
            await using var stream = await server.Resources.VirtualStorageBlobs.Stream(connection, node, null, cancellationToken);
            await foreach (VirtualStorageBlobResource blob in stream)
            {
              outHandle.Write(blob.Read().ToByteArray());
            }
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

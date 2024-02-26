using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Framework.Logging;
using Framework.Memory;

using Core;
using Resources;
using Utilities;
using Services;

public static class Program
{
  public static async Task Main()
  {
    Server server = new(new(
      DatabaseConnectionStringBuilder: new MySqlConnectionStringBuilder()
      {
        Server = "10.1.0.117",
        Database = "enderbytes",

        UserID = "test",
        Password = "test",

        AllowBatch = true
      },

      KeyGeneratorThreads: 8
    ));
    {
      StringBuilder buffer = new();
      // server.Logger.Logged += (level, scope, message, timestamp) => Console.Error.WriteLine($"-> [{timestamp} / {level}] [{scope}] {message}");

      ConsoleCancelEventHandler? onCancel = null;
      onCancel = (_, _) =>
      {
        Console.CancelKeyPress -= onCancel!;
        Console.CancelKeyPress += (_, _) => Environment.Exit(0);

        Console.Error.WriteLine("\rSERVER IS SHUTTING DOWN. PLEASE DO NOT PRESS CANCEL KEY ONE MORE TIME.");
        StopTest(server).WaitSync();
      };

      Console.CancelKeyPress += onCancel!;
    }

    Logger testLogger = new("Test");
    server.Logger.Subscribe(testLogger);

    await server.Start();
    try
    {
      try
      {
        await RunTest(testLogger, server);
      }
      catch
      {
        Handler = null;
        throw;
      }
      await server.Join();
    }
    catch (Exception exception)
    {
      try
      {
        await StopTest(server);
      }
      catch (Exception stopException)
      {
        throw new AggregateException(exception, stopException);
      }

      throw;
    }
  }

  private static event ResourceService.TransactionHandler? Handler = null;

  public static async Task StopTest(Server server) => await Task.Run(async () =>
  {
    if (Handler != null)
    {
      await server.ResourceService.Transact(Handler);
    }

    await server.Stop();
  });

  public static async Task RunTest(Logger logger, Server server) => await Task.Run(async () =>
  {
    await server.ResourceService.Transact((transaction, cancellationToken) =>
    {
      ResourceService service = transaction.ResoruceService;

      (UserResource originalUser, UserAuthenticationResource.UserAuthenticationToken originalToken) = service.Users.Create(transaction, "testuser", "Test User", "test", cancellationToken);
      Handler += (transaction, cancellationToken) => service.Users.Delete(transaction, originalUser, cancellationToken);

      (UserResource otherUser, UserAuthenticationResource.UserAuthenticationToken otherToken) = service.Users.Create(transaction, "testuser2", "Other User", "test", cancellationToken);
      Handler += (transaction, cancellationToken) => service.Users.Delete(transaction, otherUser, cancellationToken);

      logger.Info($"Eq: {CompositeBuffer.From(originalToken.Decrypt(originalUser.Encrypt(CompositeBuffer.From("Test").ToByteArray())))}");

      StorageResource storage = service.Storages.Create(transaction, originalUser, originalToken, cancellationToken);
      FileResource folder = service.Files.Create(transaction, storage, null, FileResource.FileType.Folder, "Folder2", originalToken, cancellationToken);
      FileAccessResource otherAccess = service.FileAccesses.Create(transaction, storage, folder, otherUser, FileAccessResource.FileAccessType.ReadWrite, originalToken, cancellationToken);
      FileResource file = service.Files.Create(transaction, storage, folder, FileResource.FileType.File, "Test2", otherToken, cancellationToken);
      FileSnapshotResource fileSnapshot = service.FileSnapshots.Create(transaction, storage, file, null, otherToken, cancellationToken);

      string[] units = ["", "K", "M", "G", "T"];
      string toReadable(decimal size)
      {
        int count = 0;
        while (size >= 1000)
        {
          size /= 1024;
          count++;
        }

        return $"{Math.Round(size, 2)}{units[count]}B";
      }

      return;

      void read(FileSnapshotResource fileSnapshot, string path)
      {
        using FileStream fileStream = File.OpenRead(path);

        long offset = 0;
        while (fileStream.Position < fileStream.Length)
        {
          cancellationToken.ThrowIfCancellationRequested();
          byte[] read = new byte[Random.Shared.Next(1024 * 1024 * 8)];
          int readLength = fileStream.Read(read);

          service.FileBufferMaps.Write(transaction, storage, file, fileSnapshot, offset, new(read, 0, readLength), otherToken, cancellationToken);

          Console.WriteLine(toReadable(fileStream.Position));
          offset += readLength;
        }
      }

      void write(FileSnapshotResource fileSnapshot, string path)
      {
        using FileStream fileStream = File.OpenWrite(path);
        fileStream.Position = 0;
        fileStream.SetLength(0);

        long size = service.FileBufferMaps.GetSize(transaction, storage, file, fileSnapshot, cancellationToken);

        for (long offset = 0; offset < size;)
        {
          long chunkSize = long.Min(size - offset, Random.Shared.Next(1024 * 1024 * 8));

          CompositeBuffer bytes = service.FileBufferMaps.Read(transaction, storage, file, fileSnapshot, offset, chunkSize, otherToken, cancellationToken);

          fileStream.Write(bytes.ToByteArray());
          Console.WriteLine(toReadable(fileStream.Position));
          offset += chunkSize;
        }
      }
    });
  });
}

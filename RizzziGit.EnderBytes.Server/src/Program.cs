using System.Text;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes;

using Framework.Logging;
using Framework.Memory;

using Core;
using Resources;
using Utilities;
using Services;
using System.Security.Cryptography;

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
    (StorageResource storage, FileResource folder, UserAuthenticationResource.UserAuthenticationToken token) = await server.ResourceService.Transact((transaction, cancellationToken) =>
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

      return (storage, folder, otherToken);
    });

    async Task upload(string path, FileResource? folder)
    {
      FileAttributes fileAttributes = File.GetAttributes(path);

      Console.WriteLine($"{path}");
      if (fileAttributes.HasFlag(FileAttributes.Directory))
      {
        FileResource folderResource = await server.ResourceService.Transact((transaction, cancellationToken) =>
        {
          return transaction.ResoruceService.Files.Create(transaction, storage, folder, FileResource.FileType.Folder, Path.GetFileName(path), token, cancellationToken);
        });

        foreach (string pathEntry in Directory.GetFiles(path).Concat(Directory.GetDirectories(path)))
        {
          await upload(pathEntry, folderResource);
        }
      }
      else if (fileAttributes.HasFlag(FileAttributes.Normal))
      {
        FileResource file = await server.ResourceService.Transact((transaction, cancellationToken) =>
        {
          return transaction.ResoruceService.Files.Create(transaction, storage, folder, FileResource.FileType.File, Path.GetFileName(path), token, cancellationToken);
        });

        using FileResource.CrossTransactionalFileHandle writer = await server.ResourceService.Transact((transaction, cancellationToken) =>
        {
          FileSnapshotResource fileSnapshot = transaction.ResoruceService.FileSnapshots.Create(transaction, storage, file, null, token, cancellationToken);

          return transaction.ResoruceService.Files.OpenFile(transaction, storage, file, fileSnapshot, token, FileResource.FileHandleFlags.ReadModify | FileResource.FileHandleFlags.Exclusive, cancellationToken).CrossTransactional;
        });

        using FileStream reader = File.OpenRead(path);

        while (reader.Position < reader.Length)
        {
          byte[] buffer = new byte[1024 * 1024];
          int bufferLength = reader.Read(buffer);

          await writer.Write(new(buffer, 0, bufferLength));
        }
      }
    }

    await upload("/run/media/cool/AC233/Testing", folder);
  });

  public static readonly string[] Units = ["", "K", "M", "G", "T"];
  public static string ToReadable(decimal size)
  {
    int count = 0;
    while (size >= 1000)
    {
      size /= 1024;
      count++;
    }

    return $"{Math.Round(size, 2)}{Units[count]}B";
  }
}

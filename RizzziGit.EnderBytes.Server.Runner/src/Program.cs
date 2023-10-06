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

  public static async Task Main(string[] _)
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

      // try
      // {
      //   await server.RunTransaction(async (connection, cancellationToken) =>
      //   {
      //     UserResource user = await server.Resources.Users.Create(connection, Buffer.Random(4).ToHexString(), cancellationToken);
      //     StoragePoolResource BlobStoragePool = await server.Resources.StoragePools.CreateVirtualPool(connection, user, Buffer.Random(4).ToHexString(), 0, cancellationToken);
      //   }, cancellationToken);
      // }
      // catch (Exception exception)
      // {
      //   Console.Error.WriteLine(exception);
      // }
    }
    finally
    {
      source.Dispose();
    }
  }
}

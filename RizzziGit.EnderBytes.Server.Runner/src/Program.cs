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

    try
    {
      await server.Init(source.Token);
      await server.Listen(source.Token);
    }
    catch (Exception exception)
    {
      Console.Error.WriteLine(exception);
    }
    finally
    {
      source.Dispose();
    }
  }
}

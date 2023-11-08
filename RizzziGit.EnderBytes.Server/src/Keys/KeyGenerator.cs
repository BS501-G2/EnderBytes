using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Keys;

using Resources;

using KeyEntry = (byte[] privatekey, byte[] publicKey);

public sealed class KeyGenerator(Server server) : Service("Blob Key Generator", server)
{
  public const int KEY_SIZE = 512 * 8;
  public const int MAX_CONCURRENT_GENERATOR = 4;
  public const int MAX_PREGENERATED_KEY_COUNT = 10000;

  public readonly Server Server = server;

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private readonly List<KeyEntry> WaitQueue = [];

  private void RunGenerateJob()
  {
    KeyEntry entry = Generate();
    lock (WaitQueue)
    {
      WaitQueue.Add(entry);
      Logger.Log(LogLevel.Verbose, $"Available Keys: {WaitQueue.Count}/{MAX_PREGENERATED_KEY_COUNT}");
    }
  }
  private static KeyEntry Generate()
  {
    RSACryptoServiceProvider provider = new()
    {
      KeySize = KEY_SIZE,
      PersistKeyInCsp = false
    };

    try
    {
      return (provider.ExportCspBlob(true), provider.ExportCspBlob(false));
    }
    finally
    {
      provider.Clear();
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    List<Task> tasks = [];

    try
    {
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        while ((tasks.Count < MAX_CONCURRENT_GENERATOR) && (WaitQueue.Count < MAX_PREGENERATED_KEY_COUNT))
        {
          tasks.Add(TaskFactory.StartNew(RunGenerateJob, cancellationToken));
        }

        if (tasks.Count != 0)
        {
          tasks.Remove(await Task.WhenAny(tasks));
          continue;
        }

        await Task.Delay(1000, cancellationToken);
      }
    }
    finally
    {
      if (tasks.Count != 0)
      {
        await Task.WhenAll(tasks);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;

  public KeyEntry GetNew()
  {
    lock (this)
    {
      if (WaitQueue.Count != 0)
      {
        KeyEntry entry = WaitQueue.ElementAt(0);
        WaitQueue.RemoveAt(0);
        Logger.Log(LogLevel.Verbose, $"Took 1 pregenerated key. Available Keys: {WaitQueue.Count}/{MAX_PREGENERATED_KEY_COUNT}");
        return entry;
      }
    }

    Logger.Log(LogLevel.Verbose, $"No pregenerated keys are available. Generating on the fly.");
    return Generate();
  }
}

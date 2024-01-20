using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Framework.Logging;

public sealed partial class KeyService(Server server) : Server.SubService(server, "Key Generator")
{
  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);
  public sealed record AesPair(byte[] Key, byte[] Iv);

  public const int KEY_SIZE = 512 * 8;
  public const int MAX_CONCURRENT_GENERATOR = 4;
  public const int MAX_PREGENERATED_KEY_COUNT = 10000;

  public IMongoDatabase MainDatabase => Server.MainDatabase;

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private readonly List<RsaKeyPair> WaitQueue = [];

  private void RunIndividualGenerationJob()
  {
    RsaKeyPair entry = Generate();
    lock (WaitQueue)
    {
      WaitQueue.Add(entry);
      Logger.Log(LogLevel.Verbose, $"Available Keys: {WaitQueue.Count}/{MAX_PREGENERATED_KEY_COUNT}");
    }
  }

  private static RsaKeyPair Generate()
  {
    RSACryptoServiceProvider provider = new()
    {
      KeySize = KEY_SIZE,
      PersistKeyInCsp = false
    };

    try
    {
      return new(provider.ExportCspBlob(true), provider.ExportCspBlob(false));
    }
    finally
    {
      provider.Clear();
    }
  }

  private async Task RunKeyGenerationJob(CancellationToken cancellationToken)
  {
    List<Task> tasks = [];

    try
    {
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();

        while ((tasks.Count < MAX_CONCURRENT_GENERATOR) && ((WaitQueue.Count + tasks.Count) < MAX_PREGENERATED_KEY_COUNT))
        {
          tasks.Add(TaskFactory.StartNew(RunIndividualGenerationJob, cancellationToken));
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

  protected override Task OnRun(CancellationToken cancellationToken)
  {

    return RunKeyGenerationJob(cancellationToken);
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;

  public AesPair GetNewAesPair()
  {
    byte[] key = RandomNumberGenerator.GetBytes(32);
    byte[] iv = RandomNumberGenerator.GetBytes(16);

    return new(key, iv);
  }

  public RsaKeyPair GetNewRsaKeyPair()
  {
    lock (WaitQueue)
    {
      if (WaitQueue.Count != 0)
      {
        RsaKeyPair entry = WaitQueue.ElementAt(0);
        WaitQueue.RemoveAt(0);
        Logger.Log(LogLevel.Verbose, $"Took 1 pregenerated key. Available Keys: {WaitQueue.Count}/{MAX_PREGENERATED_KEY_COUNT}");
        return entry;
      }
    }

    Logger.Log(LogLevel.Verbose, $"No pregenerated keys are available. Generating on the fly.");
    return Generate();
  }
}

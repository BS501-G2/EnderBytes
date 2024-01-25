using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Logging;

using Core;
using Utilities;

public sealed partial class KeyService(Server server) : Server.SubService(server, "Key Generator")
{
  public const int KEY_SIZE = 512;

  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);
  public sealed record AesPair(byte[] Key, byte[] Iv);

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private WaitQueue<RsaKeyPair>? PreGeneratedKeys;

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
    lock (this)
    {
      PreGeneratedKeys = new(Server.Configuration.MaxPregeneratedKeyCount);
    }

    try
    {
      async Task log()
      {
        int previousCount = 0;

        while (true)
        {
          await Task.Delay(1000, cancellationToken);

          if (previousCount != (previousCount = PreGeneratedKeys.BacklogCount))
          {
            Logger.Log(LogLevel.Debug, $"Available Keys: {PreGeneratedKeys.BacklogCount}/{PreGeneratedKeys.Capacity}");
          }
        }
      }

      await Task.WhenAll([
        log().ContinueWith((_) => Task.CompletedTask),
        .. await Task.WhenAll(Enumerable.Repeat(async () =>
        {
          Logger.Log(LogLevel.Debug, $"Key generation job started on \"{Thread.CurrentThread.Name}\" (#{Environment.CurrentManagedThreadId}).");

          try
          {
            while (true)
            {
              cancellationToken.ThrowIfCancellationRequested();

              RsaKeyPair keyPair = Generate();
              await PreGeneratedKeys.Enqueue(keyPair, cancellationToken);
            }
          }
          catch (Exception exception)
          {
            if (exception is not OperationCanceledException operationCanceledException || operationCanceledException.CancellationToken != cancellationToken)
            {
              Logger.Log(LogLevel.Debug, $"Key generation job (#{Environment.CurrentManagedThreadId}) has crashed.");
              throw;
            }
          }
          Logger.Log(LogLevel.Debug, $"Key generation job (#{Environment.CurrentManagedThreadId}) has stopped.");
        }, Server.Configuration.KeyGeneratorThreads).Select((e) => TaskFactory!.StartNew(e)))
      ]);
    }
    finally
    {
      PreGeneratedKeys = null;
    }
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return RunKeyGenerationJob(cancellationToken);
  }

  public AesPair GetNewAesPair()
  {
    byte[] key = RandomNumberGenerator.GetBytes(32);
    byte[] iv = RandomNumberGenerator.GetBytes(16);

    return new(key, iv);
  }

  public RsaKeyPair GetNewRsaKeyPair()
  {
    lock (this)
    {
      if (PreGeneratedKeys == null)
      {
        return Generate();
      }
    }

    return PreGeneratedKeys.Dequeue().WaitSync();
  }
}

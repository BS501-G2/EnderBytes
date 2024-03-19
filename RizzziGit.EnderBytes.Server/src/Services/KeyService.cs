using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;
using Commons.Logging;

using Core;
using Utilities;

public sealed partial class KeyService(Server server) : Server.SubService(server, "Key Generator")
{
  public const int KEY_SIZE = 512;

  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);

  public sealed record AesPair(byte[] Key, byte[] Iv)
  {
    public static AesPair Deserialize(byte[] Key, int offset = 0) => new(Key[offset..32], Key[(32 + offset)..48]);
    public byte[] Serialize() => [.. Key, .. Iv];

    public byte[] Encrypt(byte[] bytes)
    {
      using Aes aes = Aes.Create();
      using ICryptoTransform cryptoTransform = aes.CreateEncryptor(Key, Iv);

      return cryptoTransform.TransformFinalBlock(bytes);
    }

    public byte[] Decrypt(byte[] bytes)
    {
      using Aes aes = Aes.Create();
      using ICryptoTransform cryptoTransform = aes.CreateDecryptor(Key, Iv);

      return cryptoTransform.TransformFinalBlock(bytes);
    }
  }

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

  public RSACryptoServiceProvider GetRsaCryptoServiceProvider(byte[] cspBlob)
  {
    RSACryptoServiceProvider provider = new()
    {
      KeySize = KEY_SIZE,
      PersistKeyInCsp = false
    };

    provider.ImportCspBlob(cspBlob);
    return provider;
  }

  private async Task RunKeyGenerationJob(CancellationToken cancellationToken)
  {
    lock (this)
    {
      PreGeneratedKeys = new(Server.Configuration.MaxPregeneratedKeyCount);
    }

    try
    {
      try
      {
        await Task.WhenAll(Enumerable.Repeat(() =>
        {
          try
          {
            while (true)
            {
              PreGeneratedKeys.Enqueue(Generate(), cancellationToken).WaitSync();
            }
          }
          catch { }
        }, Server.Configuration.KeyGeneratorThreads).Append(async () =>
        {
          try
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
          catch { }
        }).Select((function) => TaskFactory!.StartNew(function)));
      }
      catch (Exception exception)
      {
        if (exception is not OperationCanceledException operationCanceledException || operationCanceledException.CancellationToken != cancellationToken)
        {
          throw;
        }
      }
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

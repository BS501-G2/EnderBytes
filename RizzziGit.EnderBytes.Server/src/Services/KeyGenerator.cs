using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Collections;
using Utilities;

public sealed class KeyGeneratorService(Server server) : Service("Key Generator", server)
{
  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);

  public abstract class Transformer(RSACryptoServiceProvider provider) : IDisposable
  {
    public void Dispose()
    {
      GC.SuppressFinalize(this);
      provider.Dispose();
    }

    public byte[] Encrypt(byte[] bytes) => provider.Encrypt(bytes, true);
    public byte[] Decrypt(byte[] bytes) => provider.Decrypt(bytes, true);
    public bool PublicOnly => provider.PublicOnly;

    public sealed class Key(RSACryptoServiceProvider provider, long sharedId) : Transformer(provider)
    {
      public readonly long SharedId = sharedId;
    }

    public sealed class UserAuthentication(RSACryptoServiceProvider provider, long userId) : Transformer(provider)
    {
      public readonly long UserId = userId;
    }
  }

  public const int KEY_SIZE = 512 * 8;
  public const int MAX_CONCURRENT_GENERATOR = 4;
  public const int MAX_PREGENERATED_KEY_COUNT = 10000;

  public readonly Server Server = server;

  public IMongoCollection<Record.Key> KeyRecords => Server.GetCollection<Record.Key>();
  public IMongoCollection<Record.UserAuthentication> UserAuthenticationRecords => Server.GetCollection<Record.UserAuthentication>();

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private readonly List<RsaKeyPair> WaitQueue = [];

  private readonly WeakDictionary<long, Transformer.Key> KeyTransformers = [];
  private readonly WeakDictionary<long, Transformer.UserAuthentication> UserAuthenticationTransformers = [];

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
    Server.UserService.UserRecords.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }

      UserAuthenticationRecords.DeleteMany(
        from userAuthentication in UserAuthenticationRecords.AsQueryable()
        where userAuthentication.UserId == change.FullDocument.Id
        select userAuthentication
      );
    }, cancellationToken);

    return RunKeyGenerationJob(cancellationToken);
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;

  public RsaKeyPair GetNew()
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

  public Transformer.UserAuthentication GetTransformer(Record.UserAuthentication userAuthentication, byte[]? hashCache)
  {
    lock (UserAuthenticationTransformers)
    {
      if (
        (!UserAuthenticationTransformers.TryGetValue(userAuthentication.UserId, out Transformer.UserAuthentication? transformer)) ||
        (hashCache != null && transformer.PublicOnly)
      )
      {
        RSACryptoServiceProvider provider = new()
        {
          PersistKeyInCsp = false,
          KeySize = KEY_SIZE
        };

        transformer = UserAuthenticationTransformers[userAuthentication.UserId] = new(provider, userAuthentication.UserId);
        provider.ImportCspBlob(hashCache != null ? userAuthentication.GetDecryptedEncryptionPrivateKey(hashCache) : userAuthentication.EncryptionPublicKey);
      }

      return transformer;
    }
  }
}

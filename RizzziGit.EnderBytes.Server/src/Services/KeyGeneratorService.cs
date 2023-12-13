using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Collections;
using Utilities;

public sealed class KeyGeneratorService(Server server) : Service("Key Generator", server)
{
  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);

  public abstract class Transformer(RSACryptoServiceProvider provider, long sharedId) : IDisposable
  {
    public void Dispose()
    {
      GC.SuppressFinalize(this);
      provider.Dispose();
    }

    public readonly long SharedId = sharedId;
    public byte[] Encrypt(byte[] bytes) => provider.Encrypt(bytes, true);
    public byte[] Decrypt(byte[] bytes) => provider.Decrypt(bytes, true);
    public bool PublicOnly => provider.PublicOnly;

    public sealed class Key(RSACryptoServiceProvider provider, long sharedId) : Transformer(provider, sharedId);
    public sealed class UserKey(RSACryptoServiceProvider provider, long sharedId) : Transformer(provider, sharedId);
  }

  public const int KEY_SIZE = 512 * 8;
  public const int MAX_CONCURRENT_GENERATOR = 4;
  public const int MAX_PREGENERATED_KEY_COUNT = 10000;

  public readonly Server Server = server;

  public IMongoCollection<Record.UserKey> UserKeys => Server.GetCollection<Record.UserKey>();
  public IMongoCollection<Record.Key> Keys => Server.GetCollection<Record.Key>();

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private readonly List<RsaKeyPair> WaitQueue = [];
  private readonly WeakDictionary<long, Transformer.Key> KeyTransformers = [];
  private readonly WeakDictionary<long, Transformer.UserKey> UserKeyTransformers = [];

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
    IMongoCollection<Record.User> users = Server.UserService.Users;

    UserKeys.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }

      Keys.DeleteMany($"{{ {nameof(Record.Key.UserKeyId)}: {change.FullDocument.Id} }}");
    }, cancellationToken);

    users.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }

      UserKeys.DeleteMany($"{{ {nameof(Record.UserKey.UserId)}: {change.FullDocument.Id} }}");
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

  public byte[] DecryptPrivateKey(Record.Key key, Transformer.UserKey? transformer)
  {
    if (key.UserKeySharedId != null)
    {
      if (transformer == null)
      {
        throw new InvalidOperationException("Requires key transformer.");
      }
      else if (transformer?.SharedId != key.UserKeySharedId)
      {
        throw new InvalidOperationException("Requires matching user key shared id.");
      }

      return transformer.Decrypt(key.PrivateKey);
    }

    return key.PrivateKey;
  }

  public Transformer.Key GetTransformer(Record.Key key, Transformer.UserKey? userKeyTransformer = null)
  {
    lock (KeyTransformers)
    {
      if (
        (!KeyTransformers.TryGetValue(key.Id, out Transformer.Key? transformer)) ||
        (userKeyTransformer != null && transformer.PublicOnly)
      )
      {
        RSACryptoServiceProvider provider = new()
        {
          PersistKeyInCsp = false,
          KeySize = KEY_SIZE
        };

        transformer = KeyTransformers[key.SharedId] = new(provider, key.SharedId);
        provider.ImportCspBlob(key.UserKeySharedId == null ? key.PrivateKey : userKeyTransformer == null ? key.PublicKey : userKeyTransformer.Decrypt(key.PrivateKey));
      }

      return transformer;
    }
  }
  public Transformer.UserKey GetTransformer(Record.UserKey userKey, byte[]? hashCache = null)
  {
    lock (UserKeyTransformers)
    {
      if (
        (!UserKeyTransformers.TryGetValue(userKey.Id, out Transformer.UserKey? transformer)) ||
        (hashCache != null && transformer.PublicOnly)
      )
      {
        RSACryptoServiceProvider provider = new()
        {
          PersistKeyInCsp = false,
          KeySize = KEY_SIZE
        };

        transformer = new(provider, userKey.SharedId);
        provider.ImportCspBlob(hashCache != null ? Aes.Create().CreateDecryptor(hashCache, userKey.PrivateIv).TransformFinalBlock(userKey.EncryptedPrivateKey) : userKey.PublicKey);
      }

      return transformer;
    }
  }

  public Task<Record.UserKey?> GetUserKey(Record.User user, Record.UserAuthentication userAuthentication) =>
    Server.MongoClient.RunTransaction(() =>
      (from userKey in UserKeys.AsQueryable() where userKey.UserId == user.Id && userAuthentication.Id == userKey.UserAuthenticationId select userKey).First()
      ?? null
    );
}

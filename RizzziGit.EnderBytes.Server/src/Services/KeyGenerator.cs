using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;
using Framework.Collections;
using Framework.Services;
using Framework.Logging;

public sealed class KeyService(Server server) : Service("Key Generator", server)
{
  public sealed record RsaKeyPair(byte[] Privatekey, byte[] PublicKey);

  public abstract class Transformer(RSACryptoServiceProvider provider) : IDisposable
  {
    ~Transformer() => Dispose();

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
  public IMongoDatabase MainDatabase => Server.MainDatabase;

  public IMongoCollection<Record.Key> Keys => MainDatabase.GetCollection<Record.Key>();
  public IMongoCollection<Record.UserAuthentication> UserAuthentications => MainDatabase.GetCollection<Record.UserAuthentication>();

  private readonly TaskFactory TaskFactory = new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
  private readonly List<RsaKeyPair> WaitQueue = [];

  private readonly WeakDictionary<long, Transformer.Key> PrivateKeyTransformers = [];
  private readonly WeakDictionary<long, Transformer.Key> PublicKeyTransformers = [];

  private readonly WeakDictionary<long, Transformer.UserAuthentication> PrivateUserAuthenticationTransformers = [];
  private readonly WeakDictionary<long, Transformer.UserAuthentication> PublicUserAuthenticationTransformers = [];

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

  public Transformer.UserAuthentication GetTransformer(Record.UserAuthentication userAuthentication, byte[]? hashCache)
  {
    WeakDictionary<long, Transformer.UserAuthentication> transformers = hashCache == null ? PublicUserAuthenticationTransformers : PrivateUserAuthenticationTransformers;

    lock (transformers)
    {
      if (!transformers.TryGetValue(userAuthentication.UserId, out Transformer.UserAuthentication? transformer))
      {
        RSACryptoServiceProvider provider = new()
        {
          PersistKeyInCsp = false,
          KeySize = KEY_SIZE
        };

        transformer = transformers[userAuthentication.UserId] = new(provider, userAuthentication.UserId);
        provider.ImportCspBlob(hashCache != null ? userAuthentication.GetDecryptedEncryptionPrivateKey(hashCache) : userAuthentication.EncryptionPublicKey);
      }

      return transformer;
    }
  }

  public Task<Transformer.Key> GetTransformer(long sharedId) => GetTransformer(null, sharedId);
  public Task<Transformer.Key> GetTransformer(Transformer.UserAuthentication? userAuthenticationTransformer, long sharedId) => RunTask(async (cancellationToken) =>
  {
    Record.Key key = await Server.MongoClient.RunTransaction(async (cancellationToken) =>
    {
      await foreach (var record in Keys.FindAsync((record) => record.SharedId == sharedId && (userAuthenticationTransformer != null ? record.UserId == userAuthenticationTransformer.UserId : record.UserId == null), cancellationToken: cancellationToken).ToAsyncEnumerable(cancellationToken))
      {
        return record;
      }

      throw new InvalidOperationException($"{(userAuthenticationTransformer != null ? "The specified user" : "The public")} does not have access to the key.");
    }, cancellationToken: cancellationToken);

    WeakDictionary<long, Transformer.Key> transformers = key.UserId == null ? PublicKeyTransformers : PrivateKeyTransformers;
    lock (transformers)
    {
      if (!transformers.TryGetValue(key.SharedId, out Transformer.Key? transformer))
      {
        RSACryptoServiceProvider provider = new()
        {
          PersistKeyInCsp = false,
          KeySize = KEY_SIZE
        };

        transformer = transformers[key.SharedId] = new(provider, key.SharedId);
        provider.ImportCspBlob(userAuthenticationTransformer?.Decrypt(key.PrivateKey) ?? key.PrivateKey);
      }

      return transformer;
    }
  });

  private async Task<Record.Key> InsertNewKey(long? userId, byte[] privateKey, byte[] publicKey, CancellationToken cancellationToken)
  {
    long sharedId;
    do
    {
      sharedId = Random.Shared.NextInt64();
    }
    while (await (await Keys.FindAsync((entry) => entry.SharedId == sharedId, cancellationToken: cancellationToken)).AnyAsync(cancellationToken));

    (long id, long createTime, long updateTime) = Record.GenerateNewId(Keys);
    Record.Key key = new(id, createTime, updateTime, sharedId, userId, privateKey, publicKey);
    await Keys.InsertOneAsync(key, cancellationToken: cancellationToken);
    return key;
  }

  public Task<Record.Key> CreateNewKey(Transformer.UserAuthentication? userAuthenticationTransformer) => RunTask((cancellationToken) =>
  {
    (byte[] privateKey, byte[] publicKey) = GetNewRsaKeyPair();

    return InsertNewKey(userAuthenticationTransformer?.UserId, userAuthenticationTransformer?.Encrypt(privateKey) ?? privateKey, publicKey, cancellationToken);
  });

  public Task<Record.Key> DuplicateKey(
    Record.Key key,
    Transformer.UserAuthentication? existingUserAuthenticationTransformer,
    Transformer.UserAuthentication? newUserAuthenticationTransformer
  ) => RunTask((cancellationToken) =>
  {
    if (key.UserId != existingUserAuthenticationTransformer?.UserId)
    {
      throw new ArgumentException("User authentication does not match the key user id.", nameof(existingUserAuthenticationTransformer));
    }

    byte[] privateKey = existingUserAuthenticationTransformer != null ? existingUserAuthenticationTransformer.Decrypt(key.PrivateKey) : key.PrivateKey;
    byte[] publicKey = key.PublicKey;

    return InsertNewKey(newUserAuthenticationTransformer?.UserId, newUserAuthenticationTransformer?.Encrypt(privateKey) ?? privateKey, publicKey, cancellationToken);
  });
}

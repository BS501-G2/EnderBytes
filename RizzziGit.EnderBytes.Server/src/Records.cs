using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Records;

using Utilities;
using Services;
using Framework.Collections;

public abstract partial record Record(long Id, long CreateTime, long UpdateTime)
{
  public static (long Id, long CreateTime, long UpdateTime) GenerateNewId<T>(IMongoCollection<T> collection) where T : Record
  {
    long id;
    do
    {
      id = Random.Shared.NextInt64();
    }
    while (collection.Find((record) => record.Id == id).FirstOrDefault() != null);

    long createTime;
    long updateTime = createTime = Random.Shared.NextInt64();

    return (id, createTime, updateTime);
  }

  public static async Task WatchRecordUpdates(MongoClient client, IMongoDatabase database, CancellationToken cancellationToken)
  {
    List<Task> tasks = [];

    await foreach (string collectionName in (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToAsyncEnumerable(cancellationToken))
    {
      tasks.Add(watch(database.GetCollection<Record>(collectionName), cancellationToken));
    }

    await await Task.WhenAny(tasks);
    async Task watch(IMongoCollection<Record> collection, CancellationToken cancellationToken)
    {
      await foreach (ChangeStreamDocument<Record> change in (await collection.WatchAsync(cancellationToken: cancellationToken)).ToAsyncEnumerable(cancellationToken))
      {
        if (
          (change.OperationType != ChangeStreamOperationType.Update) &&
          (change.OperationType != ChangeStreamOperationType.Replace)
        )
        {
          continue;
        }

        if (change.FullDocumentBeforeChange.UpdateTime == change.FullDocument.UpdateTime)
        {
          await collection.UpdateManyAsync((record) => record.Id == change.FullDocument.Id, Builders<Record>.Update.Set((e) => e.UpdateTime, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), cancellationToken: cancellationToken);
        }
      }
    }
  }

  public sealed record User(
    long Id,
    long CreateTime,
    long UpdateTime,
    string Username,
    string DisplayName
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record UserAuthentication(
    long Id,
    long CreateTime,
    long UpdateTime,

    long UserId,
    UserAuthenticationType Type,

    // Hashing Algorithm,
    HashAlgorithmName AlgorithmName,
    int Iterations,
    byte[] Salt,

    // Aes Key Challenge
    byte[] ChallengeIv,
    byte[] EncryptedChallengeBytes,
    byte[] ExpectedChallengeBytes,

    // Encryption
    byte[] EncryptionPrivateKeyIv,
    byte[] EncryptionPrivateKey,
    byte[] EncryptionPublicKey
  ) : Record(Id, CreateTime, UpdateTime)
  {
    public static byte[] GetHash(byte[] salt, int iterations, HashAlgorithmName algorithmName, string payload) => GetHash(salt, iterations, algorithmName, Encoding.UTF8.GetBytes(payload));
    public static byte[] GetHash(byte[] salt, int iterations, HashAlgorithmName algorithmName, byte[] payload) => new Rfc2898DeriveBytes(payload, salt, iterations, algorithmName).GetBytes(32);

    public byte[] GetHash(string payload) => GetHash(Salt, Iterations, AlgorithmName, payload);
    public byte[] GetHash(byte[] payload) => GetHash(Salt, Iterations, AlgorithmName, payload);

    public bool HashMatches(byte[] hash)
    {
      try
      {
        return Aes.Create().CreateDecryptor(hash, ChallengeIv).TransformFinalBlock(EncryptedChallengeBytes).SequenceEqual(ExpectedChallengeBytes);
      }
      catch
      {
        return false;
      }
    }

    public byte[] GetDecryptedEncryptionPrivateKey(byte[] hash) => Aes.Create().CreateDecryptor(hash, EncryptionPrivateKeyIv).TransformFinalBlock(EncryptionPrivateKey);

    public bool Matches(string payload) => Matches(Encoding.UTF8.GetBytes(payload));
    public bool Matches(byte[] payload) => HashMatches(GetHash(payload));
  }

  public sealed record Key(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SharedId,

    long? UserId,

    byte[] PrivateKey,
    byte[] PublicKey
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record StorageHub(
    long Id,
    long CreateTime,
    long UpdateTime,
    long OwnerUserId,
    long KeySharedId,
    StorageHubType Type,
    StorageHubFlags Flags,
    string Name
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record BlobStorageNode(
    long Id,
    long CreateTime,
    long UpdateTime,
    long BlobHubId,
    StorageHubService.NodeType Type,
    long? ParentNode,
    string Name,
    long KeySharedId
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record BlobStorageFileSnapshot(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileNodeId,
    long AuthorUserId,
    long Size,
    long? BaseSnapshotId
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record BlobStorageFileDataMapper(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SnapshotId,
    long DataId,
    long SequenceIndex
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record BlobStorageFileData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long KeySharedId,
    long Size,
    byte[] Buffer
  ) : Record(Id, CreateTime, UpdateTime)
  {
    private readonly static WeakDictionary<long, byte[]> DecryptedBuffer = [];

    public byte[] DecryptBuffer(KeyService.Transformer.Key key)
    {
      if (KeySharedId != key.SharedId)
      {
        throw new ArgumentException("Invalid key share id.", nameof(key));
      }

      lock (DecryptedBuffer)
      {
        if (!DecryptedBuffer.TryGetValue(Id, out byte[]? buffer))
        {
          DecryptedBuffer.Add(Id, buffer = key.Decrypt(Buffer));
          return buffer;
        }

        return buffer;
      }
    }
  }
}

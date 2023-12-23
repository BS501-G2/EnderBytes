using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;
using Framework.Collections;

public enum UserAuthenticationType : byte { Password, SessionToken }

public sealed partial class UserService(Server server) : Server.SubService(server, "Users")
{
  public const int SALT_SIZE = 16;
  public const int ITERATIONS = 100000;
  public const int IV_SIZE = 16;
  public const int KEY_SIZE = 32;
  public const int CHALLENGE_PAYLOAD_SIZE = 1024;

  public static readonly HashAlgorithmName DefaultHashAlgorithmName = HashAlgorithmName.SHA256;

  private readonly WeakDictionary<long, GlobalSession> Sessions = [];
  private readonly WaitQueue<(TaskCompletionSource<GlobalSession> source, long userId)> WaitQueue = new(1);

  public IMongoCollection<Record.User> Users => Server.GetCollection<Record.User>();
  public IMongoCollection<Record.UserAuthentication> UserAuthentications => Server.GetCollection<Record.UserAuthentication>();

  public async Task<Session> GetSession(Record.User user, KeyGeneratorService.Transformer.UserAuthentication transformer)
  {
    GlobalSession globalSession;
    {
      TaskCompletionSource<GlobalSession> globalSource = new();
      await WaitQueue.Enqueue((globalSource, user.Id));
      globalSession = await globalSource.Task;

      if (transformer.UserId != user.Id || transformer.PublicOnly)
      {
        throw new ArgumentException("Transformer does not match the user id.", nameof(transformer));
      }

      return await globalSession.Get(transformer, LastSessionId++);
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach (var (source, userId) in WaitQueue)
    {
      try
      {
        lock (Sessions)
        {
          if (!Sessions.TryGetValue(userId, out var value))
          {
            (value = new(this, userId)).Start(cancellationToken);
          }

          source.SetResult(Sessions[userId] = value);
        }
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    Users.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }

      UserAuthentications.DeleteMany((record) => change.FullDocument.Id == record.UserId);
    }, cancellationToken);

    Users.BeginWatching((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (Sessions.TryGetValue(change.FullDocument.Id, out GlobalSession? session))
      {
        session.Stop();
      }
    }, cancellationToken);

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;

  private Task<Record.UserAuthentication> CreateUserAuthentication(long userId, UserAuthenticationType type, byte[] salt, byte[] challengeIv, byte[] encryptedChallengeBytes, byte[] expectedChallengeBytes, byte[] privateKeyIv, byte[] privateKey, byte[] publicKey) => Server.MongoClient.RunTransaction(async (cancellationToken) =>
  {
    (long id, long createTime, long updateTime) = Record.GenerateNewId(UserAuthentications);
    Record.UserAuthentication userAuthentication = new(id, createTime, updateTime, userId, type, DefaultHashAlgorithmName, ITERATIONS, salt, challengeIv, encryptedChallengeBytes, expectedChallengeBytes, privateKeyIv, privateKey, publicKey);
    await UserAuthentications.InsertOneAsync(userAuthentication, cancellationToken: cancellationToken);
    return userAuthentication;
  });

  public Task<Record.UserAuthentication> CreateUserAuthentication(Record.User user, Record.UserAuthentication existingUserAuthentication, byte[] existingHashCache, UserAuthenticationType type, byte[] payload) => RunTask((cancellationToken) =>
  {
    if (existingUserAuthentication.UserId == user.Id)
    {
      throw new ArgumentException("User id does not match with the user authentication.", nameof(existingUserAuthentication));
    }
    else if (!existingUserAuthentication.HashMatches(existingHashCache))
    {
      throw new ArgumentException("Provided hash does not match the user authentication.");
    }

    byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
    byte[] hash = Record.UserAuthentication.GetHash(salt, ITERATIONS, DefaultHashAlgorithmName, payload);
    byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
    byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);
    byte[] encryptedChallengeBytes = Aes.Create().CreateDecryptor(hash, challengeIv).TransformFinalBlock(challengeBytes);

    byte[] privateKey = Aes.Create().CreateDecryptor(existingHashCache, existingUserAuthentication.EncryptionPrivateKeyIv).TransformFinalBlock(existingUserAuthentication.EncryptionPrivateKey);
    byte[] publicKey = existingUserAuthentication.EncryptionPublicKey;
    byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
    byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hash, privateKeyIv).TransformFinalBlock(privateKey);

    return CreateUserAuthentication(user.Id, type, salt, challengeIv, encryptedChallengeBytes, challengeBytes, privateKeyIv, privateKey, publicKey);
  });

  public Task<Record.UserAuthentication> CreateUserAuthentication(Record.User user, UserAuthenticationType type, byte[] payload) => RunTask(async (cancellationToken) =>
  {
    if (await (await UserAuthentications.FindAsync((userAuthentication) => userAuthentication.UserId == user.Id, cancellationToken: cancellationToken)).AnyAsync(cancellationToken: cancellationToken))
    {
      throw new InvalidOperationException("Must use an existing user authentication record to create a new one.");
    }

    byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
    byte[] hash = Record.UserAuthentication.GetHash(salt, ITERATIONS, DefaultHashAlgorithmName, payload);
    byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
    byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);
    byte[] encryptedChallengeBytes = Aes.Create().CreateEncryptor(hash, challengeIv).TransformFinalBlock(challengeBytes);

    (byte[] privateKey, byte[] publicKey) = Server.KeyGeneratorService.GetNewRsaKeyPair();
    byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
    byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hash, privateKeyIv).TransformFinalBlock(privateKey);

    return await CreateUserAuthentication(user.Id, type, salt, challengeIv, encryptedChallengeBytes, challengeBytes, privateKeyIv, privateKey, publicKey);
  });
}

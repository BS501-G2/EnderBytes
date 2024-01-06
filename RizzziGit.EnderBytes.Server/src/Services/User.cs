using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;
using Framework.Collections;
using Framework.Services;

public enum UserAuthenticationType : byte { Password, SessionToken }

public sealed partial class UserService(Server server) : Server.SubService(server, "Users")
{
  public const int SALT_SIZE = 16;
  public const int ITERATIONS = 100000;
  public const int IV_SIZE = 16;
  public const int KEY_SIZE = 32;
  public const int CHALLENGE_PAYLOAD_SIZE = 1024;

  public static readonly HashAlgorithmName DefaultHashAlgorithmName = HashAlgorithmName.SHA256;

  public sealed class Session(UserService service, long id, long userId) : Lifetime($"User #{userId} session #{id}")
  {
    public sealed class ConnectionBinding(Session global, ConnectionService.Connection connection, KeyService.Transformer.UserAuthentication transformer) : Lifetime($"Bindings for connection #{connection.Id}", global)
    {
      public readonly Session Session = global;
      public readonly ConnectionService.Connection Connection = connection;
      public long UserId => Session.UserId;
      public readonly KeyService.Transformer.UserAuthentication Transformer = transformer;

      public KeyService.Transformer.Key GetKeyTransformer(long keySharedId) => Session.Service.Server.KeyService.GetTransformer(Transformer, keySharedId);

      protected override async Task OnRun(CancellationToken cancellationToken)
      {
        try
        {
          lock (Session)
          {
            Session.ConnectionBindings.Add(Connection, this);
          }

          using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            Connection.GetCancellationToken()
          );

          await base.OnRun(linkedCancellationTokenSource.Token);
        }
        finally
        {
          lock (Session)
          {
            Session.ConnectionBindings.Remove(Connection);
          }
        }
      }
    }

    public readonly UserService Service = service;
    public readonly long Id = id;
    public readonly long UserId = userId;

    private readonly Dictionary<ConnectionService.Connection, ConnectionBinding> ConnectionBindings = [];

    public Record.User UserRecord => Service.UserCollection.FindOne((record) => record.Id == UserId)!;

    public ConnectionBinding GetBinding(ConnectionService.Connection connection, KeyService.Transformer.UserAuthentication transformer)
    {
      if (transformer.PublicOnly)
      {
        throw new ArgumentException("Transformer cannot be public only.", nameof(transformer));
      }

      lock (this)
      {
        if (!ConnectionBindings.TryGetValue(connection, out ConnectionBinding? connectionBinding))
        {
          connectionBinding = new(this, connection, transformer);
          connectionBinding.Start(GetCancellationToken());
        }

        return connectionBinding;
      }
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      try
      {
        lock (Service)
        {
          Service.Sessions.Add(UserId, this);
        }

        await base.OnRun(cancellationToken);
      }
      finally
      {
        lock (Service)
        {
          Service.Sessions.Remove(UserId);
        }
      }
    }
  }

  private readonly WeakDictionary<long, Session> Sessions = [];
  private long NextId = 0;

  public IMongoDatabase MainDatabase => Server.MainDatabase;

  public IMongoCollection<Record.User> UserCollection => MainDatabase.GetCollection<Record.User>();
  public IMongoCollection<Record.UserAuthentication> UserAuthenticationCollections => MainDatabase.GetCollection<Record.UserAuthentication>();

  public Session GetSession(KeyService.Transformer.UserAuthentication transformer, long? sessionId = null, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    lock (this)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!Sessions.TryGetValue(transformer.UserId, out Session? session))
      {
        session = new(this, NextId++, transformer.UserId);
        session.Start(GetCancellationToken());
      }

      return session;
    }
  }

  protected override Task OnRun(CancellationToken cancellationToken) => Task.Delay(-1, cancellationToken);
  protected override Task OnStart(CancellationToken cancellationToken)
  {
    UserCollection.Watch((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }

      UserAuthenticationCollections.DeleteMany((record) => change.FullDocument.Id == record.UserId);
    }, cancellationToken);

    UserCollection.Watch((change) =>
    {
      if (change.OperationType != ChangeStreamOperationType.Delete)
      {
        return;
      }
      else if (Sessions.TryGetValue(change.FullDocument.Id, out Session? session))
      {
        session.Stop();
      }
    }, cancellationToken);

    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception) => Task.CompletedTask;

  private Record.UserAuthentication CreateUserAuthentication(long userId, UserAuthenticationType type, byte[] salt, byte[] challengeIv, byte[] encryptedChallengeBytes, byte[] expectedChallengeBytes, byte[] privateKeyIv, byte[] privateKey, byte[] publicKey, CancellationToken cancellationToken = default)
  {
    (long id, long createTime, long updateTime) = UserAuthenticationCollections.GenerateNewId(cancellationToken);
    Record.UserAuthentication userAuthentication = new(id, createTime, updateTime, userId, type, DefaultHashAlgorithmName, ITERATIONS, salt, challengeIv, encryptedChallengeBytes, expectedChallengeBytes, privateKeyIv, privateKey, publicKey);
    UserAuthenticationCollections.InsertOne(userAuthentication, cancellationToken: cancellationToken);
    return userAuthentication;
  }

  public Record.UserAuthentication CreateUserAuthentication(Record.User user, Record.UserAuthentication existingUserAuthentication, byte[] existingHashCache, UserAuthenticationType type, byte[] payload, CancellationToken cancellationToken = default)
  {
    if (existingUserAuthentication.UserId == user.Id)
    {
      throw new ArgumentException("User id does not match with the user authentication.", nameof(existingUserAuthentication));
    }
    else if (!existingUserAuthentication.HashMatches(existingHashCache))
    {
      throw new ArgumentException("Provided hash does not match the user authentication.");
    }

    lock (this)
    {
      byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
      byte[] hash = Record.UserAuthentication.GetHash(salt, ITERATIONS, DefaultHashAlgorithmName, payload);
      byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);
      byte[] encryptedChallengeBytes = Aes.Create().CreateDecryptor(hash, challengeIv).TransformFinalBlock(challengeBytes);

      byte[] privateKey = Aes.Create().CreateDecryptor(existingHashCache, existingUserAuthentication.EncryptionPrivateKeyIv).TransformFinalBlock(existingUserAuthentication.EncryptionPrivateKey);
      byte[] publicKey = existingUserAuthentication.EncryptionPublicKey;
      byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hash, privateKeyIv).TransformFinalBlock(privateKey);

      return CreateUserAuthentication(user.Id, type, salt, challengeIv, encryptedChallengeBytes, challengeBytes, privateKeyIv, encryptedPrivateKey, publicKey, cancellationToken);
    }
  }

  public Record.UserAuthentication CreateUserAuthentication(Record.User user, UserAuthenticationType type, byte[] payload, CancellationToken cancellationToken = default)
  {
    if (UserAuthenticationCollections.FindOne((userAuthentication) => userAuthentication.UserId == user.Id, cancellationToken: cancellationToken) != null)
    {
      throw new InvalidOperationException("Must use an existing user authentication record to create a new one.");
    }

    lock (this)
    {
      byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
      byte[] hash = Record.UserAuthentication.GetHash(salt, ITERATIONS, DefaultHashAlgorithmName, payload);
      byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);
      byte[] encryptedChallengeBytes = Aes.Create().CreateEncryptor(hash, challengeIv).TransformFinalBlock(challengeBytes);

      (byte[] privateKey, byte[] publicKey) = Server.KeyService.GetNewRsaKeyPair();
      byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] encryptedPrivateKey = Aes.Create().CreateEncryptor(hash, privateKeyIv).TransformFinalBlock(privateKey);

      return CreateUserAuthentication(user.Id, type, salt, challengeIv, encryptedChallengeBytes, challengeBytes, privateKeyIv, encryptedPrivateKey, publicKey, cancellationToken);
    }
  }
}

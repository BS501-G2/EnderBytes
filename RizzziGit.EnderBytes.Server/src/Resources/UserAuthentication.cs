using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services.Resource;

public enum UserAuthenticationType { Password }

public sealed partial class UserAuthentication(UserAuthentication.ResourceManager manager, Resource<UserAuthentication.ResourceManager, UserAuthentication.ResourceData, UserAuthentication>.ResourceRecord record) : Resource<UserAuthentication.ResourceManager, UserAuthentication.ResourceData, UserAuthentication>(manager, record)
{
  public const int SALT_SIZE = 16;
  public const int ITERATIONS = 100000;
  public const int IV_SIZE = 16;
  public const int KEY_SIZE = 32;
  public const int CHALLENGE_PAYLOAD_SIZE = 1024;

  private const string NAME = "UserAuthentication";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService.Main main) : Resource<ResourceManager, ResourceData, UserAuthentication>.ResourceManager(main, main.Server.MainDatabase, NAME, VERSION)
  {
    protected override UserAuthentication CreateResourceClass(ResourceRecord record) => new(this, record);

    public UserAuthentication Replicate(User user, (UserAuthentication Authentication, byte[] PayloadHash) existing, UserAuthenticationType newType, byte[] newPayload, CancellationToken cancellationToken = default)
    {
      byte[] privateKey = existing.Authentication.GetPrivateKey(existing.PayloadHash);
      byte[] publicKey = existing.Authentication.Data.PublicKey;

      HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
      byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
      int iterations = ITERATIONS;
      byte[] payloadHash = GetPayloadHash(salt, iterations, newPayload, hashAlgorithmName);

      byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);
      byte[] challengeEncryptedBytes;
      {
        using Aes aes = Aes.Create();
        using ICryptoTransform encryptor = aes.CreateEncryptor(payloadHash, challengeIv);

        challengeEncryptedBytes = encryptor.TransformFinalBlock(challengeBytes);
      }

      byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      {
        using Aes aes = Aes.Create();
        using ICryptoTransform encryptor = aes.CreateEncryptor(payloadHash, privateKeyIv);

        privateKey = encryptor.TransformFinalBlock(privateKey);
      }

      return Run((cancellationToken) => RunTransaction((cancellationToken) => Insert(new(user.Id, newType, hashAlgorithmName, iterations, salt, challengeIv, challengeBytes, challengeEncryptedBytes, privateKeyIv, privateKey, publicKey), cancellationToken), cancellationToken: cancellationToken), cancellationToken);
    }

    public UserAuthentication Create(User user, UserAuthenticationType type, byte[] payload, CancellationToken cancellationToken = default)
    {
      HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
      byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
      int iterations = ITERATIONS;
      byte[] payloadHash = GetPayloadHash(salt, iterations, payload, hashAlgorithmName);

      byte[] challengeIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      byte[] challengeBytes = RandomNumberGenerator.GetBytes(CHALLENGE_PAYLOAD_SIZE);

      byte[] challengeEncryptedBytes;
      {
        using Aes aes = Aes.Create();
        using ICryptoTransform cryptoTransform = aes.CreateEncryptor(payloadHash, challengeIv);

        challengeEncryptedBytes = cryptoTransform.TransformFinalBlock(challengeBytes);
      }

      (byte[] privateKey, byte[] publicKey) = Server.KeyService.GetNewRsaKeyPair();
      byte[] privateKeyIv = RandomNumberGenerator.GetBytes(IV_SIZE);
      {
        using Aes aes = Aes.Create();
        using ICryptoTransform cryptoTransform = aes.CreateEncryptor(payloadHash, privateKeyIv);

        privateKey = cryptoTransform.TransformFinalBlock(privateKey);
      }

      return Run((cancellationToken) =>
      {
        return RunTransaction((cancellationToken) =>
        {
          if (Collection.FindOne((record) => record.Data.UserId == user.Id, cancellationToken: cancellationToken) != null)
          {
            throw new InvalidOperationException("Existing user authentication is needed to create a new one.");
          }

          return Insert(new(user.Id, type, hashAlgorithmName, iterations, salt, challengeIv, challengeBytes, challengeEncryptedBytes, privateKeyIv, privateKey, publicKey), cancellationToken);
        }, cancellationToken: cancellationToken);
      }, cancellationToken);
    }
  }

  public new sealed record ResourceData(
    long UserId,
    UserAuthenticationType Type,

    // Hashing Algorithm,
    HashAlgorithmName AlgorithmName,
    int Iterations,
    byte[] Salt,

    // Aes Key Challenge
    byte[] ChallengeIv,
    byte[] ChallengeBytes,
    byte[] ChallengeEncryptedBytes,

    // Encryption
    byte[] PrivateKeyIv,
    byte[] PrivateKey,
    byte[] PublicKey
  ) : Resource<ResourceManager, ResourceData, UserAuthentication>.ResourceData;

  public static RSACryptoServiceProvider CreateRSACryptoServiceProvider(byte[] cspBlob)
  {
    RSACryptoServiceProvider provider = new()
    {
      PersistKeyInCsp = false,
      KeySize = KEY_SIZE
    };

    provider.ImportCspBlob(cspBlob);
    return provider;
  }

  public static byte[] GetPayloadHash(byte[] salt, int iterations, byte[] payload, HashAlgorithmName hashAlgorithmName) => new Rfc2898DeriveBytes(payload, salt, iterations, hashAlgorithmName).GetBytes(32);

  ~UserAuthentication()
  {
    lock (this)
    {
      PrivateCryptoServiceProvider?.Dispose();
      PublicCryptoServiceProvider?.Dispose();

      PrivateCryptoServiceProvider = PublicCryptoServiceProvider = null;
    }
  }

  private RSACryptoServiceProvider? PrivateCryptoServiceProvider;
  private RSACryptoServiceProvider? PublicCryptoServiceProvider;

  public long UserId => Data.UserId;
  public UserAuthenticationType Type => Data.Type;
  public byte[] PublicKey => Data.PublicKey;

  public byte[] GetPrivateKey(byte[] payloadHash)
  {
    ThrowIfPayloadHashInvalid(payloadHash);

    using Aes aes = Aes.Create();
    using ICryptoTransform decryptor = aes.CreateDecryptor(payloadHash, Data.PrivateKeyIv);

    return decryptor.TransformFinalBlock(Data.PrivateKey);
  }

  public byte[] GetPayloadHash(byte[] payload)
  {
    byte[] payloadHash = GetPayloadHash(Data.Salt, Data.Iterations, payload, Data.AlgorithmName);

    if (!IsPayloadHashValid(payloadHash))
    {
      throw new ArgumentException("Invalid payload.", nameof(payload));
    }

    return payloadHash;
  }

  public void ThrowIfPayloadHashInvalid(byte[] payloadHash)
  {
    if (!IsPayloadHashValid(payloadHash))
    {
      throw new InvalidOperationException("Invalid payload hash.");
    }
  }

  public bool IsPayloadHashValid(byte[] payloadHash)
  {
    using Aes aes = Aes.Create();
    using ICryptoTransform cryptoTransform = aes.CreateDecryptor(payloadHash, Data.ChallengeIv);

    bool success = false;
    try
    {
      success = cryptoTransform.TransformFinalBlock(Data.ChallengeEncryptedBytes).SequenceEqual(Data.ChallengeBytes);
    }
    catch { }

    return success;
  }

  private RSACryptoServiceProvider GetRSACryptoServiceProvider(byte[]? payloadHash)
  {
    lock (this)
    {
      if (!IsValid)
      {
        throw new InvalidOperationException("Invalid user authentication.");
      }

      if (payloadHash == null)
      {
        return PublicCryptoServiceProvider ??= CreateRSACryptoServiceProvider(Data.PublicKey);
      }

      return PrivateCryptoServiceProvider ??= CreateRSACryptoServiceProvider(GetPrivateKey(payloadHash));
    }
  }

  public byte[] Encrypt(byte[] bytes)
  {
    RSACryptoServiceProvider crypto = GetRSACryptoServiceProvider(null);

    return crypto.Encrypt(bytes, false);
  }

  public byte[] Decrypt(byte[] bytes, byte[] payloadHash)
  {
    RSACryptoServiceProvider crypto = GetRSACryptoServiceProvider(payloadHash);

    return crypto.Decrypt(bytes, false);
  }
}

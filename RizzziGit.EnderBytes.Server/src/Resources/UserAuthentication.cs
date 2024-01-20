using System.Security.Cryptography;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public enum UserAuthenticationType { Password }

public sealed partial class UserAuthentication(UserAuthentication.ResourceManager manager, Resource<UserAuthentication.ResourceManager, UserAuthentication.ResourceData, UserAuthentication>.ResourceRecord record) : Resource<UserAuthentication.ResourceManager, UserAuthentication.ResourceData, UserAuthentication>(manager, record)
{
  public const int SALT_SIZE = 16;
  public const int ITERATIONS = 100000;
  public const int IV_SIZE = 16;
  public const int KEY_SIZE = 32;
  public const int CHALLENGE_PAYLOAD_SIZE = 1024;

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

  protected override void OnDestroy()
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

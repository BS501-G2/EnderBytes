using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;

public sealed partial class  UserAuthentication
{
  private const string NAME = "UserAuthentication";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService main) : Resource<ResourceManager, ResourceData, UserAuthentication>.ResourceManager(main, main.Server.MainDatabase, NAME, VERSION)
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
}

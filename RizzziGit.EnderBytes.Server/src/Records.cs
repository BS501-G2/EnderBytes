using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Records;

using Utilities;
using Services;
using Framework.Collections;

public abstract partial record Record(long Id, long CreateTime, long UpdateTime)
{
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

  public sealed record Storage(
    long Id,
    long CreateTime,
    long UpdateTime,
    long OwnerUserId,
    string Name,
    StorageService.StorageType Type,
    long KeySharedId
  ) : Record(Id, CreateTime, UpdateTime);
}

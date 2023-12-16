using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes.Records;

using Utilities;
using Services;

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
    byte[] EncryptedChallengePayload,
    byte[] ExpectedChallengePayload,

    // Encryption
    byte[] EncryptionPrivateKeyIv,
    byte[] EncryptionPrivateKey,
    byte[] EncryptionPublicKey
  ) : Record(Id, CreateTime, UpdateTime)
  {
    public byte[] GetHash(string payload) => GetHash(Encoding.UTF8.GetBytes(payload));
    public byte[] GetHash(byte[] payload) => new Rfc2898DeriveBytes(payload, Salt, Iterations, AlgorithmName).GetBytes(32);

    public bool HashMatches(byte[] hash)
    {
      try
      {
        return Aes.Create().CreateDecryptor(hash, ChallengeIv).TransformFinalBlock(EncryptedChallengePayload).SequenceEqual(ExpectedChallengePayload);
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
  ) : Record(Id, CreateTime, UpdateTime)
  {
    public sealed record BlobNode(
      long Id,
      long CreateTime,
      long UpdateTime,
      BlobNodeType Type,
      long? ParentNode,
      string Name
    ) : Record(Id, CreateTime, UpdateTime);
  }
}

using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes.Records;

using Utilities;
using Users;

public record Record(long Id, long CreateTime, long UpdateTime)
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
    int Iterations,
    byte[] Salt,
    byte[] IV,
    byte[] EncryptedChallengePayload,
    byte[] ExpectedChallengePayload
  ) : Record(Id, CreateTime, UpdateTime)
  {
    public byte[] GetHash(string payload) => GetHash(Encoding.UTF8.GetBytes(payload));
    public byte[] GetHash(byte[] payload) => new Rfc2898DeriveBytes(payload, Salt, Iterations, HashAlgorithmName.SHA256).GetBytes(32);

    public bool HashMatches(byte[] hash)
    {
      try
      {
        return Aes.Create().CreateDecryptor(hash, IV).TransformFinalBlock(EncryptedChallengePayload).SequenceEqual(ExpectedChallengePayload);
      }
      catch
      {
        return false;
      }
    }

    public bool Matches(string payload) => Matches(Encoding.UTF8.GetBytes(payload));
    public bool Matches(byte[] payload) => HashMatches(GetHash(payload));
  }

  public sealed record UserKey(
    long Id,
    long CreateTime,
    long UpdateTime,
    long UserId,
    long UserAuthenticationId,
    long SharedId,
    byte[] PrivateIv,
    byte[] EncryptedPrivateKey,
    byte[] PublicKey
  ) : Record(Id, CreateTime, UpdateTime);

  public sealed record Key(
    long Id,
    long CreateTime,
    long UpdateTime,
    long SharedId,
    long? UserKeySharedId,
    long? UserKeyId,
    byte[] PrivateKey,
    byte[] PublicKey
  ) : Record(Id, CreateTime, UpdateTime);
}

using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

public sealed partial class UserAuthentication
{
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
}

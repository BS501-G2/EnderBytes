using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class KeyService
{
  public abstract class Transformer(RSACryptoServiceProvider provider) : IDisposable
  {
    ~Transformer() => Dispose();

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      provider.Dispose();
    }

    public byte[] Encrypt(byte[] bytes) => provider.Encrypt(bytes, true);
    public byte[] Decrypt(byte[] bytes) => provider.Decrypt(bytes, true);
    public bool PublicOnly => provider.PublicOnly;

    public sealed class Key(RSACryptoServiceProvider provider, long sharedId) : Transformer(provider)
    {
      public readonly long SharedId = sharedId;
    }

    public sealed class UserAuthentication(RSACryptoServiceProvider provider, long userId) : Transformer(provider)
    {
      public readonly long UserId = userId;
    }
  }
}

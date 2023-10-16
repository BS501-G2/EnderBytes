using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Extensions;

public static class RandomNumberGeneratorExtensions
{
  public static byte[] GetBytes(this RandomNumberGenerator rng, int length)
  {
    byte[] bytes = new byte[length];
    rng.GetBytes(bytes);
    return bytes;
  }
}

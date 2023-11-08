using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Extensions;

public static class ICryptoTransformExtensions
{
  public static byte[] TransformFinalBlock(this ICryptoTransform transform, byte[] bytes) => transform.TransformFinalBlock(bytes, 0, bytes.Length);
}

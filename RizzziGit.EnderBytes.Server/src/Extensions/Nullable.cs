using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Extensions;

public static class NullableExtensions
{
  public static bool TryGetValue<T>(this T? nullable, [MaybeNullWhen(false)] out T value)
    where T : struct
  {
    if (nullable == null)
    {
      value = default;
      return false;
    }

    value = nullable.Value;
    return true;
  }
}

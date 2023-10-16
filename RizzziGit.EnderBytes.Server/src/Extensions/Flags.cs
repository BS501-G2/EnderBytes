namespace RizzziGit.EnderBytes.Extensions;

public static class FlagExtensions
{
  public static bool HasFlag<T>(this T flag, T bit) where T : Enum => (Convert.ToInt32(flag) & Convert.ToInt32(bit)) == Convert.ToInt32(bit);
  public static T AddFlag<T>(this T flag, T bit) where T : Enum => (T)Enum.ToObject(typeof(T), Convert.ToInt32(flag) | Convert.ToInt32(bit));
  public static T RemoveFlag<T>(this T flag, T bit) where T : Enum => (T)Enum.ToObject(typeof(T), Convert.ToInt32(flag) & ~Convert.ToInt32(bit));
}

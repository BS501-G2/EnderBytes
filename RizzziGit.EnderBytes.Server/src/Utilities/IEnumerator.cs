namespace RizzziGit.EnderBytes.Utilities;

public static class IEnumeratorExtensions
{
  public static IEnumerable<T> Wrap<T>(this IEnumerator<T> enumerator)
  {
    using (enumerator)
    {
      while (enumerator.MoveNext())
      {
        yield return enumerator.Current;
      }
    }
  }
}

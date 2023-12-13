using System.Runtime.CompilerServices;

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

  public static async IAsyncEnumerable<T> Wrap<T>(this MongoDB.Driver.IAsyncCursor<T> enumerator, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using (enumerator)
    {
      while (await enumerator.MoveNextAsync(cancellationToken))
      {
        foreach (T? entry in enumerator.Current)
        {
          if (entry == null)
          {
            continue;
          }

          yield return entry;
        }
      }
    }
  }
}

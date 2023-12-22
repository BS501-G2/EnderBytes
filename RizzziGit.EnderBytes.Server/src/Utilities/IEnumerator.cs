using System.Runtime.CompilerServices;
using MongoDB.Driver;

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

  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IFindFluent<T, T> cursor, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    await foreach (T entry in (await cursor.ToCursorAsync(cancellationToken)).ToAsyncEnumerable(cancellationToken))
    {
      yield return entry;
    }
  }

  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IAsyncCursor<T>> cursor, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    await foreach (T entry in (await cursor).ToAsyncEnumerable(cancellationToken))
    {
      yield return entry;
    }
  }

  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursor<T> cursor, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using (cursor)
    {
      while (await cursor.MoveNextAsync(cancellationToken))
      {
        foreach (T? entry in cursor.Current)
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

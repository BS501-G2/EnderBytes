using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Extensions;

internal static class IAsyncEnumerableExtensions
{
  public static IAsyncEnumerable<Output> Select<Input, Output>(this IAsyncEnumerable<Input> input, Func<Input, Task<Output>> callback) => input.Select((input, _) => callback(input), CancellationToken.None);
  public static async IAsyncEnumerable<Output> Select<Input, Output>(this IAsyncEnumerable<Input> input, Func<Input, CancellationToken, Task<Output>> callback, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    await foreach (Input entry in input)
    {
      yield return await callback(entry, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
    }
  }

  public static IAsyncEnumerable<Output> Select<Input, Output>(this IAsyncEnumerable<Input> input, Func<Input, Output> callback) => input.Select(callback, CancellationToken.None);
  public static async IAsyncEnumerable<Output> Select<Input, Output>(this IAsyncEnumerable<Input> input, Func<Input, Output> callback, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    await foreach (Input entry in input)
    {
      yield return callback(entry);
      cancellationToken.ThrowIfCancellationRequested();
    }
  }
}

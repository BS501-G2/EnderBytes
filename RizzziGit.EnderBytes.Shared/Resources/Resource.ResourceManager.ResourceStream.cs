


namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    protected abstract class ResourceStream<Co, E>(M manager) : IAsyncEnumerable<R>, IAsyncDisposable
      where Co : ResourceStream<Co, E>
      where E : ResourceEnumerator<Co, E>
    {
      public readonly M Manager = manager;

      public abstract Task<bool> MoveNext(CancellationToken cancellationToken);
      public abstract Task<R> GetCurrent(CancellationToken cancellationToken);

      public abstract ValueTask DisposeAsync();

      protected abstract ResourceEnumerator<Co, E> GetAsyncEnumerator(CancellationToken cancellationToken = default);
      IAsyncEnumerator<R> IAsyncEnumerable<R>.GetAsyncEnumerator(CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
    }
  }
}

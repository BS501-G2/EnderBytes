namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    public abstract class ResourceEnumerator<Co, E>(Co collection, CancellationToken cancellationToken) : IAsyncEnumerator<R>
      where Co : ResourceStream<Co, E>
      where E : ResourceEnumerator<Co, E>
    {
      protected readonly Co Stream = collection;
      protected readonly CancellationToken CancellationToken = cancellationToken;

      public abstract R Current { get; }
      public abstract ValueTask DisposeAsync();
      public abstract ValueTask<bool> MoveNextAsync();
    }
  }
}

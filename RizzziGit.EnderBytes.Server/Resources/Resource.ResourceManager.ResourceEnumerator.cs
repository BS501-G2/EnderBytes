namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    protected sealed class ResourceEnumerator(Resource<M, D, R>.ResourceManager.ResourceStream collection, CancellationToken cancellationToken) : ResourceEnumerator<ResourceStream, ResourceEnumerator>(collection, cancellationToken)
    {
      public override R Current => throw new NotImplementedException();

      public override ValueTask DisposeAsync() => Collection.DisposeAsync();

      public override ValueTask<bool> MoveNextAsync() => new(Collection.MoveNext(CancellationToken));
    }
  }
}

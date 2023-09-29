using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    protected sealed class ResourceStream(M manager, SQLiteDataReader reader) : ResourceStream<ResourceStream, ResourceEnumerator>(manager), IAsyncDisposable
    {
      public new readonly M Manager = manager;
      public readonly SQLiteDataReader Reader = reader;
      public override ValueTask DisposeAsync() => Reader.DisposeAsync();
      public override R GetCurrent() => Manager.AsResource(Manager.CreateData(Reader));
      public override Task<bool> MoveNext(CancellationToken cancellationToken) => Reader.ReadAsync(cancellationToken);
      protected override ResourceEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new(this, cancellationToken);
    }
  }
}

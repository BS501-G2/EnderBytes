namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    private Handle(Context context, StoragePool pool)
    {
      Pool = pool;
      Context = context;
    }

    public readonly StoragePool Pool;
    public readonly Context Context;

    protected abstract Task<Folder?> InternalGetParent(CancellationToken cancellationToken);
    public Task<Folder?> GetParent(CancellationToken cancellationToken) => InternalGetParent(cancellationToken);

    public abstract long Id { get; }
    public abstract Path Path { get; }
    public abstract long? AccessTime { get; }
    public abstract long? TrashTime { get; }
  }
}

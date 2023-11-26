namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    private Handle(StoragePool pool)
    {
      Pool = pool;
    }

    public readonly StoragePool Pool;

    protected abstract Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken);

    public Task<Folder?> GetParent(Context context, CancellationToken cancellationToken) => InternalGetParent(context, cancellationToken);
    public async Task<Path> GetPath(Context context, CancellationToken cancellationToken)
    {
      Folder? parent = await GetParent(context, cancellationToken);

      if (parent != null)
      {
        return new Path(Pool, [.. await GetPath(context, cancellationToken), await GetName(context, cancellationToken)]);
      }

      return new Path(Pool);
    }

    public abstract Task<string> GetName(Context context, CancellationToken cancellationToken);
    public abstract Task<long?> GetAccessTime(Context context, CancellationToken cancellationToken);
    public abstract long Id { get; }
  }
}

namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract class Folder : Handle
    {
      protected Folder(StoragePool pool) : base(pool) { }

      protected abstract IAsyncEnumerable<Handle> InternalScan(Context context, CancellationToken cancellationToken);
      protected abstract Task InternalMoveHere(Context context, Handle handle, CancellationToken cancellationToken);

      protected abstract Task<File> InternalCreateFile(Context context, string name, long preallocateLength, CancellationToken cancellationToken);
      protected abstract Task<Folder> InternalCreateFolder(Context context, string name, CancellationToken cancellationToken);
      protected abstract Task<SymbolicLink> InternalCreateSymbolicLink(Context context, string name, Path target, CancellationToken cancellationToken);

      public async Task<Handle> GetByPath(Context context, Path path, CancellationToken cancellationToken)
      {
        if (path.Length == 0)
        {
          return this;
        }

        return await (await Pool.GetRoot(context, cancellationToken)).GetByPath(context, new(Pool, [.. await GetPath(context, cancellationToken), .. path]), cancellationToken);
      }

      public async Task<Handle[]> Scan(Context context, CancellationToken cancellationToken)
      {
        List<Handle> handles = [];

        await foreach (Handle handle in InternalScan(context, cancellationToken))
        {
          handles.Add(handle);
        }

        return [.. handles];
      }

      public Task<File> CreateFile(Context context, string name, CancellationToken cancellationToken) => CreateFile(context, name, 0, cancellationToken);
      public virtual async Task<File> CreateFile(Context context, string name, long preallocateLength, CancellationToken cancellationToken)
      {
        return await InternalCreateFile(context, name, preallocateLength, cancellationToken);
      }

      // public virtual async Task<File> CreateFile(Context context, string name, File copyFromFile, CancellationToken cancellationToken)
      // {
      //   // return await InternalCreateFile(context, name, copyFromFile.Size)
      // }
    }
  }
}

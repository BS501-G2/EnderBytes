namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{

  public abstract partial class Handle
  {
    public abstract class Folder : Handle
    {
      protected Folder(Context context, StoragePool pool) : base(context, pool) { }

      protected abstract IAsyncEnumerable<Handle> InternalScan(CancellationToken cancellationToken);
      protected abstract Task InternalRemove(CancellationToken cancellationToken);

      protected abstract Task<File> InternalCreateFile(string name, long preallocateLength, CancellationToken cancellationToken);
      protected abstract Task<File> InternalCreateFile(string name, File copyFromFile, CancellationToken cancellationToken);
      protected abstract Task<Folder> InternalCreateFolder(string name, CancellationToken cancellationToken);
      protected abstract Task<Folder> InternalCreateFolder(string name, Folder copyFromFolder, CancellationToken cancellationToken);
      protected abstract Task<SymbolicLink> InternalCreateSymbolicLink(string name, Path target, CancellationToken cancellationToken);
      protected abstract Task<SymbolicLink> InternalCreateSymbolicLink(string name, SymbolicLink copyFromSymbolicLink, CancellationToken cancellationToken);

      public async Task<Handle[]> Scan(CancellationToken cancellationToken)
      {
        List<Handle> handles = [];

        await foreach (Handle handle in InternalScan(cancellationToken))
        {
          if (handle is Root)
          {
            throw new InvalidOperationException("Invalid handle type.");
          }

          handles.Add(handle);
        }

        return [.. handles];
      }

      public Task<File> CreateFile(string name, CancellationToken cancellationToken) => CreateFile(name, 0, cancellationToken);
      public async Task<File> CreateFile(string name, long preallocateLength, CancellationToken cancellationToken)
      {
        throw new NotImplementedException();
      }

      public async Task<File> CreateFile(string name, File copyFromFile, CancellationToken cancellationToken)
      {
        throw new NotImplementedException();
      }
    }
  }
}

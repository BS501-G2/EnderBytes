namespace RizzziGit.EnderBytes.StoragePools;

using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFolderHandle(StoragePool pool, FileNodeResource resource) : Handle.Folder(pool)
  {
    private readonly FileNodeResource Resource = resource;

    public override long Id => Resource.Id;

    public override Task<long?> GetAccessTime(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<string> GetName(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<File> InternalCreateFile(Context context, string name, long preallocateLength, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<File> InternalCreateFile(Context context, string name, File copyFromFile, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder> InternalCreateFolder(Context context, string name, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder> InternalCreateFolder(Context context, string name, Folder copyFromFolder, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<SymbolicLink> InternalCreateSymbolicLink(Context context, string name, Path target, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<SymbolicLink> InternalCreateSymbolicLink(Context context, string name, SymbolicLink copyFromSymbolicLink, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalRemove(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalScan(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}

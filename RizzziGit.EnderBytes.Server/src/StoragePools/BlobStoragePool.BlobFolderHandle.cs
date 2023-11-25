
namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFolderHandle(StoragePool pool) : Handle.Folder(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

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

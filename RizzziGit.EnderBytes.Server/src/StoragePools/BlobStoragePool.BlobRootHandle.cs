namespace RizzziGit.EnderBytes.StoragePools;

public sealed partial class BlobStoragePool
{
  private sealed class BlobRootHandle(StoragePool storagePool) : Root(storagePool)
  {
    protected override Task<Handle.Folder> InternalGetRootFolder(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalListTrashed(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;

public sealed class PhysicalStoragePool(EnderBytesServer server, StoragePoolResource resource) : StoragePool(server, StoragePoolResource.TYPE_PHYSICAL_POOL, resource)
{
  public override Task<Info?> Get(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task Delete(Info info, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<bool> Move(Info info, IEnumerable<string> destinationPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<DirectoryHandle> OpenDirectoryHandle(DirectoryInfo info, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<FileHandle> OpenFileHandle(FileInfo info, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

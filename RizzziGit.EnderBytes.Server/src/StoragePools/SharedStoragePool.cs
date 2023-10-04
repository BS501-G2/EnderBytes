namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Database;

public sealed class VirtualStoragePool : StoragePool
{
  public VirtualStoragePool(EnderBytesServer server, StoragePoolResource resource) : base(server, StoragePoolResource.TYPE_VIRTUAL_POOL, resource)
  {
    IsDisposed = false;
  }

  public override bool IsDisposed { get; protected set; }

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

  public override ValueTask DisposeAsync()
  {
    IsDisposed = true;

    return new ValueTask();
  }
}

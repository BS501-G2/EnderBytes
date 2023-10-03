using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.StoragePool;

public sealed class RemoteStoragePool : StoragePool
{
  public RemoteStoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : base(server, requireType, resource)
  {
  }

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

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Database;
using Buffer;

public sealed class VirtualStoragePool(EnderBytesServer server, StoragePoolResource resource) : StoragePool(server, StoragePoolResource.TYPE_VIRTUAL_POOL, resource)
{
  public MainResourceManager Resources = server.Resources;

  public override Task<FileInfo> Stats(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<uint> OpenFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DeleteFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CloseFileHandle(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task WriteToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task InsertToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task RemoveDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<IEnumerable<string>> ReadDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

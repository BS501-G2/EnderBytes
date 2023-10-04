namespace RizzziGit.EnderBytes.StoragePools;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Resources;
using RizzziGit.Buffer;

public sealed class PhysicalStoragePool(EnderBytesServer server, StoragePoolResource resource) : StoragePool(server, StoragePoolResource.TYPE_PHYSICAL_POOL, resource)
{
  public override Task CloseFileHandle(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DeleteFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task InsertToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<uint> OpenFile(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<IEnumerable<string>> ReadDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task RemoveDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<FileInfo> Stats(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task WriteToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

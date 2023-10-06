using System.Data.SQLite;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Database;
using Buffer;

public sealed class BlobStoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : StoragePool(server, requireType, resource)
{
  public override Task ChangeMode(IEnumerable<string> path, int mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task ChangeOwnership(IEnumerable<string> path, UserResource userResource, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CloseFileHandle(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateDirectory(IEnumerable<string> path, int mode, UserResource owner, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateFile(IEnumerable<string> path, int mode, UserResource owner, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task CreateSymbolicLink(IEnumerable<string> path, CancellationToken cancellationToken)
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

  public override Task<IEnumerable<FileInfo>> ReadDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<string> ReadSymbolicLink(IEnumerable<string> path, string target, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task RemoveDirectory(IEnumerable<string> path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task SeekFileHandle(uint handle, long offset, SeekOrigin seekOrigin, CancellationToken cancellationToken)
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

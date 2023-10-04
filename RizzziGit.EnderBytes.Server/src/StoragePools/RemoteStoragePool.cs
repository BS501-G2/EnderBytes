using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.StoragePools;

public sealed class RemoteStoragePool : StoragePool
{
  public RemoteStoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : base(server, requireType, resource)
  {
  }

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

  public override Task InsertToFileHandle(uint handle, Buffer.Buffer buffer, CancellationToken cancellationToken)
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

  public override Task<Buffer.Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken)
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

  public override Task WriteToFileHandle(uint handle, Buffer.Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

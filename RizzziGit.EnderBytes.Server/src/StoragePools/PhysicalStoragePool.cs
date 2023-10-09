namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;

public sealed class PhysicalStoragePool(EnderBytesServer server, StoragePoolResource resource) : StoragePool(server, StoragePoolResource.TYPE_PHYSICAL_POOL, resource)
{
  public override Task<byte> ChangeMode(string[] path, int mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> ChangeOwnership(string[] path, UserResource userResource, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CloseFileHandle(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CreateDirectory(string[] path, int mode, UserResource owner, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CreateFile(string[] path, int mode, UserResource owner, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CreateSymbolicLink(string[] path, UserResource owner, string target, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> DeleteFile(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> InsertToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, uint handle)> OpenFile(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, IEnumerable<FileInfo> entries)> ReadDirectory(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, Buffer buffer)> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, string target)> ReadSymbolicLink(string[] path, string target, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> RemoveDirectory(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> SeekFileHandle(uint handle, long offset, SeekOrigin seekOrigin, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Shutdown(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Startup(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, FileInfo? stats)> Stats(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> WriteToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

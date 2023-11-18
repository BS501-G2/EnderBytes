namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using RizzziGit.Buffer;

public sealed class BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : StoragePool(manager, resource)
{
  protected override Task<StoragePool.FileHandle> InternalOpen(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  private new class FileHandle(string[] path, StoragePool pool) : StoragePool.FileHandle(path, pool)
  {
    protected override Task<Buffer> InternalRead(long position, long size, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalTruncate(long size, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalWrite(long position, Buffer buffer, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnRun(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnStart(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnStop(Exception? exception)
    {
      throw new NotImplementedException();
    }
  }
}

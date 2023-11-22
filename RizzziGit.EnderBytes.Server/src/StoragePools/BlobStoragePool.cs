using System.Text;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Resources.BlobStorage;

public sealed class BlobStoragePool : StoragePool
{
  private new class FileHandle(StoragePool pool, Path path, StoragePool.FileHandle.FileAccess access) : StoragePool.FileHandle(pool, path, access)
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

    protected override Task OnStop(System.Exception? exception)
    {
      throw new NotImplementedException();
    }
  }

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
  }

  private readonly BlobStorageResourceManager Resources;

  protected override Task<Information> InternalStat(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalDelete(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalMove(Context context, Path fromPath, Path toPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalCopy(Context context, Path sourcePath, Path destinationPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<StoragePool.FileHandle> InternalOpen(Context context, Path path, StoragePool.FileHandle.FileAccess access, StoragePool.FileHandle.FileMode mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalCreateDirectory(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRemoveDirectory(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<Information[]> InternalScanDirectory(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalCreateSymbolicLink(Context context, Path path, Path target, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<Path> InternalReadSymbolicLink(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<long> InternalTrash(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRestore(Context context, long trashedFileId, Path? newPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

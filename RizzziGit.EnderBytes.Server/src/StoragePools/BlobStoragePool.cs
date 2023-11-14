using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Buffer;
using Database;
using Connections;
using Utilities;

public sealed class BlobStoragePool : StoragePool<BlobStoragePool.FileHandle>
{
  public new sealed class FileHandle(BlobStoragePool storagePool, string[] path, FileAccess access, FileMode mode) : StoragePool<FileHandle>.FileHandle(storagePool, path, access, mode)
  {
    protected override Task InternalClose()
    {
      throw new NotImplementedException();
    }

    protected override Task<Information.File> InternalGetInfo(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Buffer> InternalRead(long position, long length, CancellationToken cancellationToken)
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
  }

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource storagePool) : base(manager, storagePool, StoragePoolType.Blob, "Blob Storage")
  {
    Resources = new(this);
  }

  public readonly BlobStorageResourceManager Resources;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await Resources.Start();

      await base.OnRun(cancellationToken);
    }
    finally
    {
      try
      {
        await Resources.Stop();
      }
      finally
      {
        if (MarkedForDeletion)
        {
          File.Delete(BlobStorageResourceManager.GetDatabaseFilePath(Manager.Server, this));
        }
      }
    }
  }

  protected override Task<FileHandle> InternalOpen(UserKeyResource userKey, byte[] hashCache, CancellationToken cancellationToken)
  {
    return null;
  }
}

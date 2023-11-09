using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Buffer;
using Database;
using Connections;
using Utilities;

public sealed class BlobStoragePool : StoragePool
{
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

  public override Task FileCreate(UserAuthenticationResource userAuthentication, byte[] hashCache, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<long> FileOpen(string[] path, FileAccess fileAccess, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> FileRead(long handleId, long size, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileTruncate(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileWrite(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

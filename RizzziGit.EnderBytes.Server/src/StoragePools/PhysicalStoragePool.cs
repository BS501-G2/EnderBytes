namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Connections;

public sealed class PhysicalStoragePool : StoragePool
{
  public PhysicalStoragePool(StoragePoolManager manager, StoragePoolResource storagePool) : base(manager, storagePool, StoragePoolType.Physical)
  {
  }

  public override Task<StoragePoolResult> Execute(Connection connection, StoragePoolCommand command, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

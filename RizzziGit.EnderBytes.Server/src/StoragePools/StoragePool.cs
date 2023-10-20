namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Utilities;

public abstract class StoragePoolConnection<Sp, SpC> : Lifetime
  where Sp : StoragePool<Sp, SpC>
  where SpC : StoragePoolConnection<Sp, SpC>
{
  protected StoragePoolConnection(Sp pool) : base("SP: ")
  {
    Pool = pool;

    pool.Logger.Subscribe(Logger);
  }

  public readonly Sp Pool;
}

public abstract class StoragePool<Sp, SpC> : Lifetime
  where Sp : StoragePool<Sp, SpC>
  where SpC : StoragePoolConnection<Sp, SpC>
{
  protected StoragePool(StoragePoolManager manager, StoragePoolResource storagePool) : base($"#{storagePool.Id}")
  {
    Manager = manager;

    Manager.Logger.Subscribe(Logger);
  }

  public readonly StoragePoolManager Manager;
}

namespace RizzziGit.EnderBytes.StoragePools;

using Utilities;

public abstract class StoragePoolConnection<Sp, SpC> : Lifetime
  where Sp : StoragePool<Sp, SpC>
  where SpC : StoragePoolConnection<Sp, SpC>
{
  protected StoragePoolConnection(Sp pool)
  {
    Pool = pool;
  }

  public readonly Sp Pool;
}

public abstract class StoragePool<Sp, SpC> : Service
  where Sp : StoragePool<Sp, SpC>
  where SpC : StoragePoolConnection<Sp, SpC>
{
  protected StoragePool(StoragePoolManager manager, string? name) : base(name)
  {
    Manager = manager;
  }

  public readonly StoragePoolManager Manager;
}
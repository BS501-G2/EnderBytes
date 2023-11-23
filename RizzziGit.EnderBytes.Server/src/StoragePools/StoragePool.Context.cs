namespace RizzziGit.EnderBytes.StoragePools;

using Connections;

public abstract partial class StoragePool
{
  public sealed class Context(StoragePool pool, Connection connection)
  {
    public readonly StoragePool Pool = pool;
    public readonly Connection Connection = connection;
  }
}

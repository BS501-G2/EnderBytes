namespace RizzziGit.EnderBytes.StoragePools;

using Connections;

public abstract partial class StoragePool
{
  public abstract partial class Context(Connection connection)
  {
    public readonly Connection Connection = connection;
  }
}

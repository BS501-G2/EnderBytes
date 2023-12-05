namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class TrashItem
  {
    public abstract Node Target { get; }
    public abstract long TrashTime { get; }
  }
}

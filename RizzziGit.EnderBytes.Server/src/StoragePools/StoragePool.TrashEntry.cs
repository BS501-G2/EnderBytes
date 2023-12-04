namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract class TrashEntry
  {
    public abstract long Id { get; }
    public abstract Node Entry { get; }
  }
}

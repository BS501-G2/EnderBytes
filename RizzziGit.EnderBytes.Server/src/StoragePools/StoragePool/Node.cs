namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Node
  {
    private Node() { }

    public abstract StoragePool Handle { get; }

    public abstract long CreateTime { get; }
    public abstract long AccessTime { get; }
    public abstract long ModifyTime { get; }

    public abstract long UserId { get; }
    public abstract long KeyId { get; }
    public abstract string Name { get; }
    public abstract Folder? Parent { get; }
  }
}

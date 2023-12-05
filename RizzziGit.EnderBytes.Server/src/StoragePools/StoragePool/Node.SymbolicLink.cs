namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Node
  {
    public abstract class SymbolicLink : Node
    {
      public abstract Path? Target { get; }
    }
  }
}

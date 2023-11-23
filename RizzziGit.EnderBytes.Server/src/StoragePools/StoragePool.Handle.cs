namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    private Handle(StoragePool pool, Folder? parent)
    {
      Pool = pool;
      Parent = parent;
    }

    public readonly StoragePool Pool;
    public readonly Handle? Parent;

    public abstract long Id { get; }
    public abstract Path Path { get; }
    public abstract long? AccessTime { get; }
    public abstract long? TrashTime { get; }
  }
}

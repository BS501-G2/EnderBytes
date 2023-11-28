namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public sealed class TrashedHandle(Handle handle, long trashTime)
  {
    public readonly Handle Handle = handle;
    public readonly long TrashTime = trashTime;
  }
}

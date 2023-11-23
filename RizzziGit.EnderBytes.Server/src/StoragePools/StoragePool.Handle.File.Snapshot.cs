namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract partial class File
    {
      public abstract class Snapshot(File file)
      {
        public readonly File File = file;

        public abstract long Id { get; }
        public abstract long? CreateTime { get; }
        public abstract long? UpdateTime { get; }
        public abstract long? AccessTime { get; }

        public abstract Task<Snapshot?> GetParentSnapshot();

        public Task<Stream> Open(Access access, Mode mode, CancellationToken cancellationToken) => File.Open(this, access, mode, cancellationToken);
      }
    }
  }
}

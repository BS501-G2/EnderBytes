// namespace RizzziGit.EnderBytes.Resources;

// using Framework.Memory;

// using Services;

// public sealed partial class FileResource
// {
//   public abstract record FileHandle(ResourceManager Manager, StorageResource Storage, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags)
//   {
//     public long Position => InternalGetPosition();

//     protected abstract long InternalGetPosition();

//     public abstract bool InternalSeek(ResourceService.Transaction transaction, long newPosition, CancellationToken cancellationToken = default);
//     public abstract long InternalGetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default);

//     public abstract CompositeBuffer Read(ResourceService.Transaction transaction, int size, CancellationToken cancellationToken = default);
//     public abstract void Write(ResourceService.Transaction transaction, CompositeBuffer buffer, CancellationToken cancellationToken = default);
//   }
// }

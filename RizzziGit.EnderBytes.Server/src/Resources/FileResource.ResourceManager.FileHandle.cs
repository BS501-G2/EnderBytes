namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Services;

public sealed partial class FileResource
{
  public sealed partial class ResourceManager
  {
    private sealed record FileHandle : FileResource.FileHandle, IDisposable
    {
      private sealed record CurrentMap(FileBufferMapResource FileBufferMap, long CurrentPosition);

      public FileHandle(ResourceManager Manager, StorageResource Storage, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags) : base(Manager, Storage, File, Snapshot, UserAuthenticationToken, Flags)
      {
        Manager.Handles.Add(this);
      }

      private bool Disposed = false;

      public override void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

      public override void Dispose()
      {
        lock (this)
        {
          if (Disposed)
          {
            return;
          }

          Disposed = true;
          Manager.Handles.Remove(this);
          GC.SuppressFinalize(this);
        }
      }

      private new long Position = 0;
      private long? CurrentSize;

      protected override long InternalGetPosition() => Position;

      private long InternalGetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
      {
        return transaction.ResourceService.GetResourceManager<FileBufferMapResource.ResourceManager>().GetSize(transaction, Storage, File, Snapshot, cancellationToken);
      }

      public override long GetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          if (Flags.HasFlag(FileHandleFlags.Exclusive))
          {
            return CurrentSize ??= InternalGetSize(transaction, cancellationToken);
          }

          return InternalGetSize(transaction, cancellationToken);
        }
      }

      public override void Seek(ResourceService.Transaction transaction, long position, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          ArgumentOutOfRangeException.ThrowIfGreaterThan(position, GetSize(transaction, cancellationToken), nameof(position));
          Position = position;
        }
      }

      public override CompositeBuffer Read(ResourceService.Transaction transaction, int size, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          if (!Flags.HasFlag(FileHandleFlags.Read))
          {
            throw new InvalidOperationException($"File handle does not have {nameof(FileHandleFlags.Read)} flag.");
          }

          CompositeBuffer buffer = transaction.ResourceService.GetResourceManager<FileBufferMapResource.ResourceManager>().Read(transaction, Storage, File, Snapshot, Position, size, UserAuthenticationToken, cancellationToken);
          Position += buffer.Length;
          return buffer;
        }
      }

      public override void Write(ResourceService.Transaction transaction, CompositeBuffer buffer, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          if (!Flags.HasFlag(FileHandleFlags.Modify))
          {
            throw new InvalidOperationException($"File handle does not have {nameof(FileHandleFlags.Modify)} flag.");
          }

          transaction.ResourceService.GetResourceManager<FileBufferMapResource.ResourceManager>().Write(transaction, Storage, File, Snapshot, Position, buffer, UserAuthenticationToken, cancellationToken);
          Position += buffer.Length;
          CurrentSize = InternalGetSize(transaction, cancellationToken);
        }
      }

      public override void Truncate(ResourceService.Transaction transaction, long size, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          if (!Flags.HasFlag(FileHandleFlags.Modify))
          {
            throw new InvalidOperationException($"File handle does not have {nameof(FileHandleFlags.Modify)} flag.");
          }

          transaction.ResourceService.GetResourceManager<FileBufferMapResource.ResourceManager>().Truncate(transaction, Storage, File, Snapshot, size, UserAuthenticationToken, cancellationToken);
          CurrentSize = InternalGetSize(transaction, cancellationToken);
        }
      }
    }
  }
}

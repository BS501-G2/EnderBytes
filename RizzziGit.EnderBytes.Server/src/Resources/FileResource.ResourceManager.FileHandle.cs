namespace RizzziGit.EnderBytes.Resources;

using Framework.Memory;

using Services;

public sealed partial class FileResource
{
  public sealed partial class ResourceManager
  {
    private sealed record FileHandle : FileResource.FileHandle, IDisposable
    {
      private const int BUFFER_SIZE = 4_096;

      private sealed record CurrentMap(FileBufferMapResource FileBufferMap, long CurrentPosition);

      public FileHandle(ResourceManager Manager, StorageResource Storage, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags) : base(Manager, Storage, File, Snapshot, UserAuthenticationToken, Flags)
      {
        Manager.Handles.Add(this);
      }

      ~FileHandle()
      {
        Manager.Handles.Remove(this);
      }

      private bool Disposed = false;

      private FileBufferMapResource.ResourceManager BufferMaps => Manager.Service.FileBufferMaps;
      private FileBufferResource.ResourceManager Buffers => Manager.Service.FileBuffers;

      private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

      public void Dispose()
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
      private long? Size;

      protected override long InternalGetPosition() => Position;

      public override long InternalGetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          if (Size != null)
          {
            return (long)Size;
          }

          (_, _, ResourceService service, _) = transaction;

          long size = service.FileBufferMaps.List(transaction, Snapshot, 0, cancellationToken).Sum((fileBufferMap) => fileBufferMap.Length);

          Size = size;
          return size;
        }
      }

      public override bool InternalSeek(ResourceService.Transaction transaction, long newPosition, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          Position = newPosition;
          return true;
        }
      }

      public override CompositeBuffer Read(ResourceService.Transaction transaction, long size, CancellationToken cancellationToken = default)
      {
        lock (this)
        {
          ThrowIfDisposed();

          CompositeBuffer buffer = [];

          long beginIndex = Position / BUFFER_SIZE;
          long beginOffset = Position - (BUFFER_SIZE * beginIndex);

          long remaining() => size - buffer.Length;

          foreach (FileBufferMapResource fileBufferMap in transaction.ResoruceService.FileBufferMaps.List(transaction, Snapshot, Position / BUFFER_SIZE, cancellationToken))
          {
            FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, fileBufferMap.Id, cancellationToken);
            StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, Storage, File, UserAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

            byte[] decrypted = decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer);

            if (beginOffset > 0)
            {
              beginOffset = 0;
            }

            if (buffer.Length >= size)
            {
              break;
            }
          }

          return buffer;
        }
      }
    }
  }
}

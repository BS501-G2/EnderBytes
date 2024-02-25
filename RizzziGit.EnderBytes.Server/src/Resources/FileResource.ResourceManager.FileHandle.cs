// namespace RizzziGit.EnderBytes.Resources;

// using Framework.Memory;

// using Services;

// public sealed partial class FileResource
// {
//   public sealed partial class ResourceManager
//   {
//     private sealed record FileHandle : FileResource.FileHandle, IDisposable
//     {
//       private const int BUFFER_SIZE = 4_096;

//       private sealed record CurrentMap(FileBufferMapResource FileBufferMap, long CurrentPosition);

//       public FileHandle(ResourceManager Manager, StorageResource Storage, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags) : base(Manager, Storage, File, Snapshot, UserAuthenticationToken, Flags)
//       {
//         Manager.Handles.Add(this);
//       }

//       ~FileHandle()
//       {
//         Manager.Handles.Remove(this);
//       }

//       private bool Disposed = false;

//       private FileBufferMapResource.ResourceManager BufferMaps => Manager.Service.FileBufferMaps;
//       private FileBufferResource.ResourceManager Buffers => Manager.Service.FileBuffers;

//       private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Disposed, this);

//       public void Dispose()
//       {
//         lock (this)
//         {
//           if (Disposed)
//           {
//             return;
//           }

//           Disposed = true;
//           Manager.Handles.Remove(this);
//           GC.SuppressFinalize(this);
//         }
//       }

//       private new long Position = 0;
//       private long? Size;

//       protected override long InternalGetPosition() => Position;

//       public override long InternalGetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
//       {
//         lock (this)
//         {
//           ThrowIfDisposed();

//           if (Size != null)
//           {
//             return (long)Size;
//           }

//           (_, _, _, ResourceService service, _) = transaction;

//           long size = service.FileBufferMaps.List(transaction, Snapshot, 0, cancellationToken).Sum((fileBufferMap) => fileBufferMap.Length);

//           Size = size;
//           return size;
//         }
//       }

//       public override bool InternalSeek(ResourceService.Transaction transaction, long newPosition, CancellationToken cancellationToken = default)
//       {
//         lock (this)
//         {
//           ThrowIfDisposed();

//           Position = newPosition;
//           return true;
//         }
//       }

//       public override CompositeBuffer Read(ResourceService.Transaction transaction, int size, CancellationToken cancellationToken = default)
//       {
//         lock (this)
//         {
//           ThrowIfDisposed();

//           CompositeBuffer buffer = [];

//           int beginIndex = (int)(Position / BUFFER_SIZE);

//           int remaining() => (int)(size - buffer.Length);

//           StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, Storage, File, UserAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

//           foreach (FileBufferMapResource fileBufferMap in transaction.ResoruceService.FileBufferMaps.List(transaction, Snapshot, Position / BUFFER_SIZE, cancellationToken))
//           {
//             FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, fileBufferMap.Id, cancellationToken);
//             byte[] decrypted = decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer);

//             buffer.Append(decrypted[(fileBufferMap.Index == beginIndex ? (int)(Position - (BUFFER_SIZE * beginIndex)) : 0)..int.Min(remaining(), fileBufferMap.Length)]);

//             if (buffer.Length >= size)
//             {
//               break;
//             }
//           }

//           Position += buffer.Length;
//           return buffer;
//         }
//       }

//       public override void Write(ResourceService.Transaction transaction, CompositeBuffer buffer, CancellationToken cancellationToken = default)
//       {
//         lock (this)
//         {
//           ThrowIfDisposed();

//           int beginIndex = (int)(Position / BUFFER_SIZE);
//           StorageResource.DecryptedKeyInfo decryptedKeyInfo = transaction.ResoruceService.Storages.DecryptKey(transaction, Storage, File, UserAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

//           CompositeBuffer decrypted = [];
//           int beginOffset = 0;

//           foreach (FileBufferMapResource fileBufferMap in transaction.ResoruceService.FileBufferMaps.List(transaction, Snapshot, Position / BUFFER_SIZE, cancellationToken))
//           {
//             FileBufferResource fileBuffer = transaction.ResoruceService.FileBuffers.GetById(transaction, fileBufferMap.Id, cancellationToken);
//             if (fileBufferMap.Index == beginIndex)
//             {
//               beginOffset = (int)(Position - (beginIndex * BUFFER_SIZE));
//             }

//             decrypted.Append(decryptedKeyInfo.Key.Decrypt(fileBuffer.Buffer));

//             if (decrypted.Length >= buffer.Length)
//             {
//               break;
//             }
//           }
//         }
//       }
//     }
//   }
// }

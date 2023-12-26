using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Framework.Memory;
using RizzziGit.EnderBytes.Utilities;

public enum BlobNodeType { File, Folder, SymbolicLink }

public sealed partial class StorageHubService
{
  public const int BUFFER_SIZE = KeyGeneratorService.KEY_SIZE / 8;

  public abstract partial class Hub
  {
    public sealed partial class Blob(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Hub(service, hubId, hubKey)
    {
      public new sealed partial class FileHandle(Blob hub, long fileId, long snapshotId, long size, KeyGeneratorService.Transformer.Key transformer, HubFileAccess access) : Hub.FileHandle(hub, fileId, snapshotId, transformer, access)
      {
        private new readonly Blob Hub = hub;

        private new long Position = 0;
        private new long Size = size;

        protected override long Internal_Position => Position;
        protected override long Internal_Size => Size;

        protected override Task<CompositeBuffer> Internal_Read(long size) => RunTask(async (cancellationToken) =>
        {
          CompositeBuffer buffer = CompositeBuffer.Empty();

          long requestSize = long.Min(size, Size - Position);
          long requestRemaining() => requestSize - buffer.Length;
          {
            int index = (int)(BUFFER_SIZE / Position);
            int offset = (int)(Position - (BUFFER_SIZE * index));
          }

          return buffer;
        });

        protected override Task Internal_Seek(long position) => RunTask((cancellationToken) =>
        {
          ArgumentOutOfRangeException.ThrowIfGreaterThan(Size, position);
          Position = position;

          return Task.CompletedTask;
        });

        protected override Task Internal_SetSize(long size) => RunTask(async (cancellationToken) =>
        {
          if (Size != size)
          {
            long endIndex = size / BUFFER_SIZE;
            await Hub.FileSnapshots.UpdateManyAsync((fileSnapshot) => fileSnapshot.Id == SnapshotId, Builders<Record.BlobStorageFileSnapshot>.Update.Set((record) => record.Size, size), cancellationToken: cancellationToken);
            await Hub.FileDataMappers.DeleteManyAsync((fileDataMapper) => fileDataMapper.SnapshotId == SnapshotId && fileDataMapper.SequenceIndex > endIndex, cancellationToken: cancellationToken);
            Position = long.Min(Position, Size = size);
          }
        });

        protected override Task Internal_Write(CompositeBuffer buffer) => RunTask(async (cancellationToken) =>
        {
          buffer = buffer.Clone();
        });
      }
    }
  }
}

namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Framework.Memory;

public sealed partial class StorageHubService
{
  public abstract partial class Hub : Lifetime
  {
    public abstract partial class FileHandle : Lifetime
    {

      public abstract class BufferCache
      {
        public delegate Task<List<BufferCache>?> TraverseCallback(BufferCache cache);

        private BufferCache(FileHandle handle, long begin, long end)
        {
          Handle = handle;
          Begin = begin;
          End = end;
        }

        public readonly FileHandle Handle;
        public readonly long Begin;
        public readonly long End;

        public long Length => End - Begin;

        public bool Synced { get; private set; } = true;

        public virtual (BufferCache Left, BufferCache Right) Split(long position) => (
          new Empty(Handle, Begin, position),
          new Empty(Handle, position, End)
        );

        public virtual (BufferCache Left, BufferCache Center, BufferCache Right) Split(long position1, long position2) => (
          new Empty(Handle, Begin, position1),
          new Empty(Handle, position1, position2),
          new Empty(Handle, position2, End)
        );

        public sealed class Empty(FileHandle handle, long begin, long end) : BufferCache(handle, begin, end);
        public sealed class Buffered(FileHandle handle, long begin, CompositeBuffer buffer) : BufferCache(handle, begin, begin + buffer.Length)
        {
          public readonly CompositeBuffer Buffer = buffer.Clone();
          public long BufferOffset(long position) => position - Begin;

          public Task Sync()
          {
            if (!Synced)
            {
              return Task.CompletedTask;
            }

            return Handle.Internal_Write(Buffer);
          }

          public void Write(long begin, CompositeBuffer buffer)
          {
            Buffer.Write(BufferOffset(begin), buffer);
            Synced = false;
          }

          public CompositeBuffer Read(long begin, long end) => Buffer.Read(BufferOffset(begin), end - begin);

          public override (BufferCache Left, BufferCache Right) Split(long position) => new(
            new Buffered(Handle, Begin, Buffer.Slice(BufferOffset(Begin), BufferOffset(position))),
            new Buffered(Handle, position, Buffer.Slice(BufferOffset(position), BufferOffset(End)))
          );

          public override (BufferCache Left, BufferCache Center, BufferCache Right) Split(long position1, long position2) => new(
            new Buffered(Handle, Begin, Buffer.Slice(BufferOffset(Begin), BufferOffset(position1))),
            new Buffered(Handle, position1, Buffer.Slice(BufferOffset(position1), BufferOffset(position2))),
            new Buffered(Handle, position2, Buffer.Slice(BufferOffset(position2), BufferOffset(End)))
          );
        }
      }
    }
  }
}

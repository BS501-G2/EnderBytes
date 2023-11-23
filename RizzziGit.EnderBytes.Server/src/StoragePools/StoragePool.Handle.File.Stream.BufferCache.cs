namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract partial class File
    {
      public abstract partial class Stream
      {
        public sealed class BufferCache(Buffer buffer, long begin, long end, bool toSync, long lastAccessTime, Func<BufferCache, Task> syncCallback)
        {
          public readonly long Begin = begin;
          public readonly long End = end;
          private readonly Buffer BufferBackingField = buffer;

          public Buffer Buffer
          {
            get
            {
              LastAccessTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
              return BufferBackingField;
            }
          }

          public bool ToSync { get; private set; } = toSync;
          public long LastAccessTime { get; private set; } = lastAccessTime;

          public long Length => End - Begin;
          public async Task Sync()
          {
            if (!ToSync)
            {
              return;
            }

            await syncCallback(this);
          }

          public void Write(long position, Buffer buffer)
          {
            ToSync = true;
            Buffer.Write(position, buffer);
          }

          public (BufferCache left, BufferCache right) Split(long index) => (
            new(Buffer.Slice(0, index), Begin, Begin + index, ToSync, LastAccessTime, syncCallback),
            new(Buffer.Slice(index, End), Begin + index, End, ToSync, LastAccessTime, syncCallback)
          );

          public (BufferCache left, BufferCache center, BufferCache right) Split(long index1, long index2) => (
            new(Buffer.Slice(0, index1), Begin, Begin + index1, ToSync, LastAccessTime, syncCallback),
            new(Buffer.Slice(index1, index2), Begin + index1, Begin + index2, ToSync, LastAccessTime, syncCallback),
            new(Buffer.Slice(index2, End), Begin + index2, End, ToSync, LastAccessTime, syncCallback)
          );
        }
      }
    }
  }
}

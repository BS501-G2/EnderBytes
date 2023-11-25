namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Utilities;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract partial class File
    {
      public abstract partial class Stream : IAsyncDisposable
      {
        protected Stream(File file, Snapshot snapshot, Access access)
        {
          Disposed = false;
          File = file;
          Snapshot = snapshot;
          TaskQueue = new();
          Access = access;
        }

        public readonly File File;
        public readonly Snapshot Snapshot;
        public readonly Access Access;

        private readonly TaskQueue TaskQueue;
        private List<BufferCache>? Cache;
        private bool Disposed;

        public long Position { get; private set; }
        public long Size { get; private set; }

        public async Task<List<BufferCache>> GetCache() => Cache ??= await File.GetCacheList(Snapshot);

        public async Task<long> GetCacheSize(Context context)
        {
          long size = 0;

          List<BufferCache> cache = await GetCache();

          lock (cache)
          {
            foreach (BufferCache entry in cache)
            {
              size += entry.Length;
            }
          }

          return size;
        }

        protected abstract Task<Buffer> InternalRead(Context context, long position, long size);
        protected abstract Task InternalWrite(Context context, long position, Buffer buffer);
        protected abstract Task InternalTruncate(Context context, long size);
        protected abstract Task InternalClose();

        private BufferCache CreateBufferCache(Context context, Buffer buffer, long begin, long end, bool toSync) => new(buffer, begin, end, toSync, default, (cache) => CacheSync(context, cache));
        private async Task CacheSync(Context context, BufferCache cache) => await InternalWrite(context, cache.Begin, cache.Buffer);

        public Task Sync() => TaskQueue.RunTask(async (_) =>
        {
          List<BufferCache> cache = await GetCache();
          List<Task> tasks = [];

          lock (cache)
          {
            foreach (BufferCache cacheEntry in cache)
            {
              tasks.Add(cacheEntry.Sync());
            }
          }

          await Task.WhenAll(tasks);
        }, CancellationToken.None);

        public Task<Buffer> Read(Context context, long length, CancellationToken cancellationToken) => TaskQueue.RunTask(async (__) =>
        {
          if (!Access.HasFlag(Access.Read))
          {
            throw new InvalidOperationException("Read flag is not present.");
          }

          long requestBegin = Position;
          long requestEnd = Position + length > Size ? Size - Position : length;
          long requestLength() => requestEnd - requestBegin;

          List<Task<Buffer>> toRead = [];
          List<BufferCache> cache = await GetCache();
          lock (cache)
          {
            for (int index = 0; index < cache.Count && requestBegin < requestEnd; index++)
            {
              BufferCache entry = cache.ElementAt(index);

              if (requestBegin >= entry.End)
              {
                continue;
              }

              if (requestBegin < entry.Begin)
              {
                long toReadLength = long.Min(entry.Begin - requestBegin, requestLength());

                Task<Buffer> bufferTask = InternalRead(context, requestBegin, toReadLength);
                BufferCache newCache = CreateBufferCache(context, Buffer.Allocate(toReadLength), requestBegin, entry.Begin, false);

                _ = bufferTask.ContinueWith(async (task) => newCache.Buffer.Write(0, await task));

                toRead.Add(bufferTask);
                cache.Insert(index, newCache);

                requestBegin += toReadLength;
              }
              else
              {
                long sliceBegin = requestBegin - entry.Begin;
                long sliceEnd = sliceBegin + long.Min(requestLength(), entry.Length - sliceBegin);

                Buffer sliced = entry.Buffer.Slice(sliceBegin, sliceEnd);
                toRead.Add(Task.FromResult(sliced));

                requestBegin += sliced.Length;
              }
            }

            if (requestBegin < requestEnd)
            {
              Task<Buffer> bufferTask = InternalRead(context, requestBegin, requestLength());
              BufferCache newCache = CreateBufferCache(context, Buffer.Allocate(requestLength()), requestBegin, requestLength(), false);

              _ = bufferTask.ContinueWith(async (task) => newCache.Buffer.Write(0, await task));

              toRead.Add(bufferTask);
              cache.Add(newCache);

              requestBegin = requestEnd;
            }
          }

          Position = requestEnd;
          return Buffer.Concat(await Task.WhenAll(toRead));
        }, cancellationToken);

        public Task Write(Context context, Buffer buffer) => TaskQueue.RunTask(async (_) =>
        {
          if (!Access.HasFlag(Access.Write))
          {
            throw new InvalidOperationException("Write flag is not present.");
          }

          Buffer toWrite = buffer.Clone();

          long requestBegin() => Position + (buffer.Length - toWrite.Length);

          List<BufferCache> cache = await GetCache();
          lock (cache)
          {
            for (int index = 0; index < cache.Count && toWrite.Length != 0; index++)
            {
              BufferCache entry = cache.ElementAt(index);

              if (requestBegin() >= entry.End)
              {
                continue;
              }

              if (requestBegin() < entry.Begin)
              {
                BufferCache newCache = CreateBufferCache(context, toWrite.TruncateStart(entry.Begin - requestBegin()), requestBegin(), entry.Begin, true);

                cache.Insert(index, newCache);
              }
              else if (entry.ToSync)
              {
                long spliceIndex = requestBegin() - entry.Begin;

                entry.Write(spliceIndex, toWrite.TruncateStart(long.Min(entry.Length - spliceIndex, toWrite.Length)));
              }
              else
              {
                if (entry.Begin == requestBegin())
                {
                  if (entry.Length > toWrite.Length)
                  {
                    var (left, right) = entry.Split(toWrite.Length);

                    left.Write(0, toWrite.TruncateStart(toWrite.Length));

                    cache.RemoveAt(index);
                    cache.Insert(index++, left);
                    cache.Insert(index, right);
                  }
                  else
                  {
                    entry.Write(0, toWrite.TruncateStart(entry.Length));
                  }
                }
                else
                {
                  long spliceIndex1 = requestBegin() - entry.Begin;

                  if ((entry.Length - spliceIndex1) <= toWrite.Length)
                  {
                    var (left, right) = entry.Split(spliceIndex1);

                    right.Write(0, toWrite.TruncateStart(right.Length));

                    cache.RemoveAt(index);
                    cache.Insert(index++, left);
                    cache.Insert(index, right);
                  }
                  else
                  {
                    long spliceIndex2 = spliceIndex1 + toWrite.Length;

                    var (left, center, right) = entry.Split(spliceIndex1, spliceIndex1 + toWrite.Length);

                    center.Write(0, toWrite.TruncateStart(toWrite.Length));

                    cache.RemoveAt(index);
                    cache.Insert(index++, left);
                    cache.Insert(index++, center);
                    cache.Insert(index, right);
                  }
                }
              }
            }
          }

          Size = long.Max(Position += buffer.Length, Size);
        }, CancellationToken.None);

        public Task Seek(long position, SeekOrigin origin) => origin switch
        {
          SeekOrigin.Current => Seek(Position + position, SeekOrigin.Begin),
          SeekOrigin.End => Seek(Size - position, SeekOrigin.Begin),
          _ => TaskQueue.RunTask(() =>
          {
            if (position < 0 || position > Size)
            {
              throw new ArgumentOutOfRangeException(nameof(position));
            }

            Position = long.Min(Size, position);
          }),
        };

        public Task Truncate(Context context, long length) => TaskQueue.RunTask(async (cancellationToken) =>
        {
          List<BufferCache> cache = await GetCache();
          lock (cache)
          {
            for (int index = 0; index < cache.Count; index++)
            {
              BufferCache entry = cache.ElementAt(index);

              if (entry.Begin >= length)
              {
                cache.RemoveAt(index--);
                continue;
              }

              if (entry.End > length)
              {
                var (left, right) = entry.Split(length - entry.End);

                cache.RemoveAt(index);
                cache.Insert(index, left);
              }
            }
          }

          Size = length;
          await Sync();
          await InternalTruncate(context, length);
        }, CancellationToken.None);

        public async ValueTask DisposeAsync()
        {
          await TaskQueue.RunTask(async (_) =>
          {
            ObjectDisposedException.ThrowIf(Disposed, this);
            GC.SuppressFinalize(this);

            await InternalClose();
            Disposed = true;
          }, CancellationToken.None);
        }
      }
    }
  }
}

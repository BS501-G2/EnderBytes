namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Framework.Memory;
using Framework.Collections;

public sealed partial class StorageHubService
{
  public abstract partial class Hub : Lifetime
  {
    public abstract partial class FileHandle : Lifetime
    {
      private static void ThrowIfFlagIsMissing(HubFileAccess flag, HubFileAccess test)
      {
        if (!flag.HasFlag(test))
        {
          throw new InvalidOperationException($"{test} access flag is not present in the handle.");
        }
      }

      public FileHandle(Hub hub, long fileId, long snapshotId, KeyGeneratorService.Transformer.Key transformer, HubFileAccess access) : base($"File #{fileId} Stream #{snapshotId}", hub)
      {
        Hub = hub;
        HandleId = hub.Service.LastFileHandleId++;
        FileId = fileId;
        SnapshotId = snapshotId;
        Transformer = transformer;
        Access = access;

        lock (hub.FileHandleCache)
        {
          if (!hub.FileHandleCache.TryGetValue(this, out List<BufferCache>? cache))
          {
            hub.FileHandleCache.Add(this, cache = []);
          }
          Cache = cache;
        }

        NodeHandles = [];
        SnapshotHandles = [];
      }

      private readonly List<BufferCache> Cache;
      private readonly WeakDictionary<long, Node> NodeHandles;
      private readonly WeakDictionary<long, Node.File.Snapshot> SnapshotHandles;

      public readonly Hub Hub;
      public readonly long HandleId;
      public readonly long FileId;
      public readonly long SnapshotId;
      public readonly HubFileAccess Access;

      protected readonly KeyGeneratorService.Transformer.Key Transformer;
      protected abstract long Internal_Position { get; }
      protected abstract long Internal_Size { get; }

      public long Position { get; private set; }
      public long Size { get; private set; }
      public long CacheSize => Cache.Select((cache) => cache is BufferCache.Buffered ? cache.Length : 0).Sum();

      protected abstract Task<CompositeBuffer> Internal_Read(long size);
      protected abstract Task Internal_Write(CompositeBuffer buffer);
      protected abstract Task Internal_Seek(long position);
      protected abstract Task Internal_SetSize(long size);

      private async Task<CompositeBuffer> Internal_Read(long begin, long end)
      {
        if (Internal_Position != begin)
        {
          await Internal_Seek(begin);
        }

        return await Internal_Read(end - begin);
      }

      private async Task Internal_Write(long begin, CompositeBuffer buffer)
      {
        if (Internal_Position != begin)
        {
          await Internal_Seek(begin);
        }

        await Internal_Write(buffer);
      }

      private async Task TraverseCache(BufferCache.TraverseCallback callback, long? hintBegin = null, long? hintEnd = null)
      {
        if (Cache.Count == 0)
        {
          Cache.Add(new BufferCache.Empty(this, 0, Size));
        }

        long cacheSize = Cache.Select((cache) => cache.Length).Sum();
        if (cacheSize < Size)
        {
          Cache.Add(new BufferCache.Empty(this, cacheSize, Size));
        }

        for (int index = 0; index < Cache.Count; index++)
        {
          BufferCache cache = Cache[index];

          if (
            ((hintEnd != null) && (hintEnd <= cache.Begin)) ||
            ((hintBegin != null) && (hintBegin >= cache.End))
          )
          {
            continue;
          }

          void processResult(List<BufferCache>? result)
          {
            if (result == null || result.Count == 0)
            {
              return;
            }

            if (result.Count == 1)
            {
              Cache[index] = result[0];
              return;
            }

            Cache.RemoveAt(index--);
            Cache.InsertRange(index + 1, result);
            index += result.Count;
          }

          if (cache.End <= Size)
          {
            processResult(await callback(cache));
          }
          else if (cache.Begin >= Size)
          {
            Cache.RemoveAt(index--);
          }
          else
          {
            (Cache[index], _) = cache.Split(Size);
            processResult(await callback(Cache[index]));
          }
        }
      }

      public Task<CompositeBuffer> Read(long size) => RunTask(async (__) =>
      {
        ThrowIfFlagIsMissing(Access, HubFileAccess.Read);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);

        CompositeBuffer output = CompositeBuffer.Empty();

        long requestBegin() => Position + output.Length;
        long requestEnd() => long.Min(Position + size, Size);

        await TraverseCache(async (cache) =>
        {
          if (cache is BufferCache.Buffered bufferedCache)
          {
            output.Append(bufferedCache.Read(long.Max(cache.Begin, requestBegin()), long.Min(cache.End, requestEnd())));
          }
          else if (requestBegin() == cache.Begin)
          {
            long end = long.Min(cache.End, requestEnd());

            CompositeBuffer buffer = await Internal_Read(cache.Begin, end);
            output.Append(buffer);

            (_, BufferCache right) = cache.Split(end);
            return [new BufferCache.Buffered(this, cache.Begin, buffer), right];
          }
          else if (requestEnd() == cache.End)
          {
            long begin = long.Max(cache.Begin, requestBegin());

            CompositeBuffer buffer = await Internal_Read(begin, cache.End);
            output.Append(buffer);

            (BufferCache left, _) = cache.Split(begin);
            return [left, new BufferCache.Buffered(this, begin, buffer)];
          }

          return null;
        }, requestBegin(), requestEnd());

        Position = requestEnd();
        return output;
      });

      public Task Write(CompositeBuffer buffer) => RunTask(async (cancellationToken) =>
      {
        ThrowIfFlagIsMissing(Access, HubFileAccess.Write);

        CompositeBuffer remainingBytes = buffer.Clone();
        remainingBytes.CopyOnWrite = true;

        long requestBegin() => Position + (buffer.Length - remainingBytes.Length);
        long requestEnd() => requestBegin() + remainingBytes.Length;

        if (buffer.Length == 0)
        {
          return;
        }

        await TraverseCache((cache) =>
        {
          if (cache is BufferCache.Buffered bufferedCache)
          {
            long begin = long.Max(requestBegin(), cache.Begin);
            long end = long.Min(requestEnd(), cache.End);

            bufferedCache.Write(begin, remainingBytes.TruncateStart(end - begin));
          }
          else if (requestBegin() == cache.Begin)
          {
            long end = long.Min(cache.End, requestEnd());

            (_, BufferCache right) = cache.Split(end);

            BufferCache.Buffered left = new(this, cache.Begin, CompositeBuffer.Allocate(end - cache.Begin));
            left.Write(left.Begin, remainingBytes.TruncateStart(end - cache.Begin));

            return Task.FromResult<List<BufferCache>?>([left, right]);
          }
          else if (requestBegin() == cache.End)
          {
            long begin = long.Max(cache.Begin, requestBegin());

            (BufferCache left, _) = cache.Split(begin);

            BufferCache.Buffered right = new(this, begin, CompositeBuffer.Allocate(cache.End - begin));
            right.Write(right.Begin, remainingBytes.TruncateStart(cache.End - begin));

            return Task.FromResult<List<BufferCache>?>([left, right]);
          }

          return Task.FromResult<List<BufferCache>?>(null);
        }, requestBegin(), requestEnd());

        Position = requestEnd();
      });

      public Task Seek(long position) => RunTask((_) =>
      {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(position, Size, nameof(position));

        Position = position;
        return Task.CompletedTask;
      });

      public Task Sync() => RunTask(async (_) =>
      {
        if (Internal_Size != Size)
        {
          await Internal_SetSize(Size);
        }

        await Task.WhenAll(sync());
        IEnumerable<Task> sync()
        {
          foreach (BufferCache cache in Cache)
          {
            if (cache is BufferCache.Buffered bufferedCache)
            {
              yield return bufferedCache.Sync();
            }
          }
        }
      });

      public Task SetSize(long size) => RunTask((_) =>
      {
        Position = long.Min(Position, Size = size);
        return Task.CompletedTask;
      });

      protected override async Task OnRun(CancellationToken cancellationToken)
      {
        try
        {
          await base.OnRun(cancellationToken);
        }
        finally
        {
          await Sync();
        }
      }
    }
  }
}
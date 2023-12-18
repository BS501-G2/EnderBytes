namespace RizzziGit.EnderBytes.Services;

using Collections;
using Buffer;
using Utilities;

[Flags]
public enum HubFileAccess : byte
{
  Read = 1 << 0,
  Write = 1 << 1,
  Exclusive = 1 << 2,

  ReadWrite = Read | Write,
  ExclusiveReadWrite = Exclusive | ReadWrite
}

[Flags]
public enum HubFileMode : byte
{
  TruncateToZero = 1 << 0,
  Append = 1 << 1
}

public sealed partial class StorageHubService
{
  public abstract partial class Hub(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Lifetime($"{hubId}", service.Logger)
  {
    public abstract record NodeInformation(long Id, long CreateTime, long UpdateTime, string Name)
    {
      public sealed record Folder(long Id, long CreateTime, long UpdateTime, string Name) : NodeInformation(Id, CreateTime, UpdateTime, Name);
      public sealed record SymbolicLink(long Id, long CreateTime, long UpdateTime, string Name) : NodeInformation(Id, CreateTime, UpdateTime, Name);
      public sealed record File(long Id, long CreateTime, long UpdateTime, string Name) : NodeInformation(Id, CreateTime, UpdateTime, Name)
      {
        public sealed record Snapshot(long Id, long? BaseId, long CreateTime, long UpdateTime);
      }
    }

    public sealed record TashItem(
      long Id,
      long CreateTime,
      NodeInformation Node
    );

    public abstract class FileHandle(Hub hub, long fileId, long snapshotId, KeyGeneratorService.Transformer.Key transformer) : Lifetime($"File #{fileId} Stream #{snapshotId}", hub)
    {
      public abstract class LazyBuffer
      {
        public delegate Task<List<LazyBuffer>?> TraverseCallback(LazyBuffer cache);

        private LazyBuffer(FileHandle handle, long begin, long end)
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

        public virtual (LazyBuffer Left, LazyBuffer Right) Split(long position) => (
          new Empty(Handle, Begin, position),
          new Empty(Handle, position, End)
        );

        public virtual (LazyBuffer Left, LazyBuffer Center, LazyBuffer Right) Split(long position1, long position2) => (
          new Empty(Handle, Begin, position1),
          new Empty(Handle, position1, position2),
          new Empty(Handle, position2, End)
        );

        public sealed class Empty(FileHandle handle, long begin, long end) : LazyBuffer(handle, begin, end);
        public sealed class Buffered(FileHandle handle, long begin, Buffer buffer) : LazyBuffer(handle, begin, begin + buffer.Length)
        {
          public readonly Buffer Buffer = buffer.Clone();
          public long BufferOffset(long position) => position - Begin;

          public Task Sync()
          {
            if (!Synced)
            {
              return Task.CompletedTask;
            }

            return Handle.Internal_Write(Begin, Buffer);
          }

          public void Write(long begin, Buffer buffer)
          {
            Buffer.Write(BufferOffset(begin), buffer);
            Synced = false;
          }

          public Buffer Read(long begin, long end) => Buffer.Read(BufferOffset(begin), end - begin);

          public override (LazyBuffer Left, LazyBuffer Right) Split(long position) => new(
            new Buffered(Handle, Begin, Buffer.Slice(BufferOffset(Begin), BufferOffset(position))),
            new Buffered(Handle, position, Buffer.Slice(BufferOffset(position), BufferOffset(End)))
          );

          public override (LazyBuffer Left, LazyBuffer Center, LazyBuffer Right) Split(long position1, long position2) => new(
            new Buffered(Handle, Begin, Buffer.Slice(BufferOffset(Begin), BufferOffset(position1))),
            new Buffered(Handle, position1, Buffer.Slice(BufferOffset(position1), BufferOffset(position2))),
            new Buffered(Handle, position2, Buffer.Slice(BufferOffset(position2), BufferOffset(End)))
          );
        }
      }

      private readonly KeyGeneratorService.Transformer.Key Transformer = transformer;
      private readonly List<LazyBuffer> Cache = [];

      public readonly Hub Hub = hub;
      public readonly long FileId = fileId;
      public readonly long SnapshotId = snapshotId;

      protected abstract long Internal_Position { get; }
      protected abstract long Internal_Size { get; }

      public long Position { get; private set; }
      public long Size { get; private set; }
      public long CacheSize => Cache.Select((cache) => cache is LazyBuffer.Buffered ? cache.Length : 0).Sum();

      protected abstract Task<Buffer> Internal_Read(long size);
      protected abstract Task Internal_Write(Buffer buffer);
      protected abstract Task Internal_Seek(long position);
      protected abstract Task Internal_SetSize(long size);

      private async Task<Buffer> Internal_Read(long begin, long end)
      {
        if (Internal_Position != begin)
        {
          await Internal_Seek(begin);
        }

        return await Internal_Read(end - begin);
      }

      private async Task Internal_Write(long begin, Buffer buffer)
      {
        if (Internal_Position != begin)
        {
          await Internal_Seek(begin);
        }

        await Internal_Write(buffer);
      }

      private async Task TraverseCache(LazyBuffer.TraverseCallback callback, long? hintBegin = null, long? hintEnd = null)
      {
        if (Cache.Count == 0)
        {
          Cache.Add(new LazyBuffer.Empty(this, 0, Size));
        }

        long cacheSize = Cache.Select((cache) => cache.Length).Sum();
        if (cacheSize < Size)
        {
          Cache.Add(new LazyBuffer.Empty(this, cacheSize, Size));
        }

        for (int index = 0; index < Cache.Count; index++)
        {
          LazyBuffer cache = Cache[index];

          if (
            ((hintEnd != null) && (hintEnd <= cache.Begin)) ||
            ((hintBegin != null) && (hintBegin >= cache.End))
          )
          {
            continue;
          }

          void processResult(List<LazyBuffer>? result)
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

        for (int index = 1; index < Cache.Count; index++)
        {
          LazyBuffer cache1 = Cache[index - 1];
          LazyBuffer cache2 = Cache[index];

          if (cache1.Synced == cache2.Synced)
          {
            if (cache1 is LazyBuffer.Buffered bufferedCache1 && cache2 is LazyBuffer.Buffered bufferedCache2)
            {
              Cache.RemoveAt(index--);
              Cache.RemoveAt(index);

              Cache.Insert(index, new LazyBuffer.Buffered(this, bufferedCache1.Begin, Buffer.Concat(bufferedCache1.Buffer, bufferedCache2.Buffer)));
            }
            else if (cache1 is LazyBuffer.Empty && cache2 is LazyBuffer.Empty)
            {
              Cache.RemoveAt(index--);
              Cache.RemoveAt(index);

              Cache.Insert(index, new LazyBuffer.Empty(this, cache1.Begin, cache2.End));
            }
          }
        }
      }

      public Task<Buffer> Read(long size) => RunTask(async (__) =>
      {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);

        Buffer output = Buffer.Empty();

        long requestBegin() => Position + output.Length;
        long requestEnd() => long.Min(Position + size, Size);

        await TraverseCache(async (cache) =>
        {
          if (cache is LazyBuffer.Buffered bufferedCache)
          {
            output.Append(bufferedCache.Read(long.Max(cache.Begin, requestBegin()), long.Min(cache.End, requestEnd())));
          }
          else if (requestBegin() == cache.Begin)
          {
            long end = long.Min(cache.End, requestEnd());

            Buffer buffer = await Internal_Read(cache.Begin, end);
            output.Append(buffer);

            (_, LazyBuffer right) = cache.Split(end);
            return [new LazyBuffer.Buffered(this, cache.Begin, buffer), right];
          }
          else if (requestEnd() == cache.End)
          {
            long begin = long.Max(cache.Begin, requestBegin());

            Buffer buffer = await Internal_Read(begin, cache.End);
            output.Append(buffer);

            (LazyBuffer left, _) = cache.Split(begin);
            return [left, new LazyBuffer.Buffered(this, begin, buffer)];
          }

          return null;
        }, requestBegin(), requestEnd());

        Position = requestEnd();
        return output;
      });

      public Task Write(Buffer buffer) => RunTask(async (cancellationToken) =>
      {
        Buffer remainingBytes = buffer.Clone();
        remainingBytes.CopyOnWrite = true;

        long requestBegin() => Position + (buffer.Length - remainingBytes.Length);
        long requestEnd() => requestBegin() + remainingBytes.Length;

        if (buffer.Length == 0)
        {
          return;
        }

        await TraverseCache((cache) =>
        {
          if (cache is LazyBuffer.Buffered bufferedCache)
          {
            long begin = long.Max(requestBegin(), cache.Begin);
            long end = long.Min(requestEnd(), cache.End);

            bufferedCache.Write(begin, remainingBytes.TruncateStart(end - begin));
          }
          else if (requestBegin() == cache.Begin)
          {
            long end = long.Min(cache.End, requestEnd());

            (_, LazyBuffer right) = cache.Split(end);

            LazyBuffer.Buffered left = new(this, cache.Begin, Buffer.Allocate(end - cache.Begin));
            left.Write(left.Begin, remainingBytes.TruncateStart(end - cache.Begin));

            return Task.FromResult<List<LazyBuffer>?>([left, right]);
          }
          else if (requestBegin() == cache.End)
          {
            long begin = long.Max(cache.Begin, requestBegin());

            (LazyBuffer left, _) = cache.Split(begin);

            LazyBuffer.Buffered right = new(this, begin, Buffer.Allocate(cache.End - begin));
            right.Write(right.Begin, remainingBytes.TruncateStart(cache.End - begin));

            return Task.FromResult<List<LazyBuffer>?>([left, right]);
          }

          return Task.FromResult<List<LazyBuffer>?>(null);
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
          foreach (LazyBuffer cache in Cache)
          {
            if (cache is LazyBuffer.Buffered bufferedCache)
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

    public readonly StorageHubService Service = service;
    public readonly long HubId = hubId;
    public readonly KeyGeneratorService.Transformer.Key HubKey = hubKey;

    private readonly WeakDictionary<long, FileHandle> FileHandles = [];

    protected abstract Task<long> Internal_ResolveNodeId(string[] path);

    protected abstract Task<NodeInformation> Internal_NodeInfo(long nodeId);
    protected abstract Task Internal_NodeDelete(long nodeId);

    protected abstract Task<NodeInformation.Folder> Internal_FolderCreate(long parentFolderNodeId, string name);
    protected abstract Task<NodeInformation[]> Internal_FolderScan(long folderNodeId);

    protected abstract Task<NodeInformation.File> Internal_FileCreate(long parentFolderNodeId, string name);
    protected abstract Task<NodeInformation.File.Snapshot> Internal_FileSnapshotCreate(long fileNodeId, long? baseSnapshotId, long authorUserId);
    protected abstract Task<NodeInformation.File.Snapshot[]> Internal_FileSnapshotScan(long fileNodeId);
    protected abstract Task<FileHandle> Internal_FileOpen(long fileNodeId, long snapshotId, HubFileAccess access);

    protected abstract Task<NodeInformation.SymbolicLink> Internal_SymbolicLinkCreate(long parentFolderNodeId, string[] target, bool replace);
    protected abstract Task<string[]> Internal_SymbolicLinkRead(long symbolicLinkNodeId);

    public Task<long> ResolveNodeId(string[] path) => Internal_ResolveNodeId(path);

    public Task<NodeInformation> NodeInfo(long nodeId) => Internal_NodeInfo(nodeId);
    public Task NodeDelete(long nodeId) => Internal_NodeDelete(nodeId);

    public Task<NodeInformation.Folder> FolderCreate(long parentFolderNodeId, string name) => Internal_FolderCreate(parentFolderNodeId, name);
    public Task<NodeInformation[]> FolderScan(long folderNodeId) => Internal_FolderScan(folderNodeId);

    public Task<NodeInformation.File> FileCreate(long parentFolderNodeId, string name) => Internal_FileCreate(parentFolderNodeId, name);
    public Task<NodeInformation.File.Snapshot> FileSnapshotCreate(long fileNodeId, long? baseSnapshotId, long authorUserId) => Internal_FileSnapshotCreate(fileNodeId, baseSnapshotId, authorUserId);
    public Task<NodeInformation.File.Snapshot[]> FileSnapshotScan(long fileNodeId) => Internal_FileSnapshotScan(fileNodeId);
    public async Task<FileHandle> FileOpen(long fileNode, long snapshotId, HubFileAccess access, HubFileMode mode)
    {
      FileHandle handle = await Internal_FileOpen(fileNode, snapshotId, access);

      if (mode.HasFlag(HubFileMode.TruncateToZero))
      {
        await handle.SetSize(0);
      }

      if (mode.HasFlag(HubFileMode.Append))
      {
        await handle.Seek(handle.Size);
      }

      return handle;
    }

    public Task<NodeInformation.SymbolicLink> SymbolicLinkCreate(long parentFolderNodeId, string[] target, bool replace = false) => Internal_SymbolicLinkCreate(parentFolderNodeId, target, replace);
    public Task<string[]> SymbolicLinkRead(long symbolicLinkNodeId) => Internal_SymbolicLinkRead(symbolicLinkNodeId);
  }
}

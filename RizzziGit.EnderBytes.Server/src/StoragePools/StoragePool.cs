using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.StoragePools;

using Utilities;
using Buffer;
using Resources;
using Collections;

public abstract class StoragePool : Service
{
  public abstract record Information(long? AccessTime, long? TrashTime)
  {
    public sealed record File(long Size, long? AccessTime, long? TrashTime) : Information(AccessTime, TrashTime);
    public sealed record Folder(long? AccessTime, long? TrashTime) : Information(AccessTime, TrashTime);
    public sealed record SymbolicLink(long? AccessTime, long? TrashTime) : Information(AccessTime, TrashTime);
  }

  public abstract class FileHandle : Service
  {
    [Flags]
    public enum FileAccess : byte
    {
      Read = 1 << 0,
      Write = 1 << 1,

      ReadWrite = Read | Write
    }

    [Flags]
    public enum FileMode : byte
    {
      Open = 1 << 0,
      Truncate = 1 << 1,
      Append = 1 << 2,
      CreateNew = 1 << 3,

      Create = Open | CreateNew | Truncate,
      OpenOrCreate = Open | CreateNew | Append
    }

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

    protected FileHandle(string[] path, StoragePool pool, FileAccess access) : base(string.Join("/", path), pool)
    {
      Pool = pool;
      Cache = pool.AcquireHandleBufferList(path);
      TaskQueue = new();
      Access = access;
    }

    public readonly StoragePool Pool;
    public readonly FileAccess Access;

    private readonly List<BufferCache> Cache;
    private readonly TaskQueue TaskQueue;

    public long Position { get; private set; }
    public long Size { get; private set; }
    public long CacheSize
    {
      get
      {
        long size = 0;

        lock (Cache)
        {
          foreach (BufferCache cache in Cache)
          {
            size += cache.Length;
          }
        }

        return size;
      }
    }

    protected abstract Task<Buffer> InternalRead(long position, long size, CancellationToken cancellationToken);
    protected abstract Task InternalWrite(long position, Buffer buffer, CancellationToken cancellationToken);
    protected abstract Task InternalTruncate(long size, CancellationToken cancellationToken);

    private BufferCache CreateBufferCache(Buffer buffer, long begin, long end, bool toSync) => new(buffer, begin, end, toSync, default, CacheSync);
    private async Task CacheSync(BufferCache cache) => await InternalWrite(cache.Begin, cache.Buffer, CancellationToken.None);

    protected override Task OnRun(CancellationToken cancellationToken) => TaskQueue.Start(cancellationToken);
    protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
    protected override Task OnStop(Exception? exception) => Sync();

    public Task Sync() => TaskQueue.RunTask(async (_) =>
    {
      List<Task> tasks = [];

      lock (Cache)
      {
        foreach (BufferCache cacheEntry in Cache)
        {
          tasks.Add(cacheEntry.Sync());
        }
      }

      await Task.WhenAll(tasks);
    }, CancellationToken.None);

    public Task<Buffer> Read(long length, CancellationToken cancellationToken) => TaskQueue.RunTask(async (cancellationToken) =>
    {
      long requestBegin = Position;
      long requestEnd = Position + length > Size ? Size - Position : length;
      long requestLength() => requestEnd - requestBegin;

      List<Task<Buffer>> toRead = [];
      lock (Cache)
      {
        for (int index = 0; index < Cache.Count && requestBegin < requestEnd; index++)
        {
          BufferCache cache = Cache.ElementAt(index);

          if (requestBegin >= cache.End)
          {
            continue;
          }

          if (requestBegin < cache.Begin)
          {
            long toReadLength = long.Min(cache.Begin - requestBegin, requestLength());

            Task<Buffer> bufferTask = InternalRead(requestBegin, toReadLength, cancellationToken);
            BufferCache newCache = CreateBufferCache(Buffer.Allocate(toReadLength), requestBegin, cache.Begin, false);

            _ = bufferTask.ContinueWith(async (task) => newCache.Buffer.Write(0, await task), cancellationToken);

            toRead.Add(bufferTask);
            Cache.Insert(index, newCache);

            requestBegin += toReadLength;
          }
          else
          {
            long sliceBegin = requestBegin - cache.Begin;
            long sliceEnd = sliceBegin + long.Min(requestLength(), cache.Length - sliceBegin);

            Buffer sliced = cache.Buffer.Slice(sliceBegin, sliceEnd);
            toRead.Add(Task.FromResult(sliced));

            requestBegin += sliced.Length;
          }
        }

        if (requestBegin < requestEnd)
        {
          Task<Buffer> bufferTask = InternalRead(requestBegin, requestLength(), cancellationToken);
          BufferCache newCache = CreateBufferCache(Buffer.Allocate(requestLength()), requestBegin, requestLength(), false);

          _ = bufferTask.ContinueWith(async (task) => newCache.Buffer.Write(0, await task), cancellationToken);

          toRead.Add(bufferTask);
          Cache.Add(newCache);

          requestBegin = requestEnd;
        }
      }

      Position = requestEnd;
      return Buffer.Concat(await Task.WhenAll(toRead));
    }, cancellationToken);

    public Task Write(Buffer buffer)
    {
      Buffer toWrite = buffer.Clone();

      long requestBegin() => Position + (buffer.Length - toWrite.Length);
      lock (Cache)
      {
        for (int index = 0; index < Cache.Count && toWrite.Length != 0; index++)
        {
          BufferCache cacheEntry = Cache.ElementAt(index);

          if (requestBegin() >= cacheEntry.End)
          {
            continue;
          }

          if (requestBegin() < cacheEntry.Begin)
          {
            BufferCache newCache = CreateBufferCache(toWrite.TruncateStart(cacheEntry.Begin - requestBegin()), requestBegin(), cacheEntry.Begin, true);

            Cache.Insert(index, newCache);
          }
          else if (cacheEntry.ToSync)
          {
            long spliceIndex = requestBegin() - cacheEntry.Begin;

            cacheEntry.Write(spliceIndex, toWrite.TruncateStart(long.Min(cacheEntry.Length - spliceIndex, toWrite.Length)));
          }
          else
          {
            if (cacheEntry.Begin == requestBegin())
            {
              if (cacheEntry.Length > toWrite.Length)
              {
                var (left, right) = cacheEntry.Split(toWrite.Length);

                left.Write(0, toWrite.TruncateStart(toWrite.Length));

                Cache.RemoveAt(index);
                Cache.Insert(index++, left);
                Cache.Insert(index, right);
              }
              else
              {
                cacheEntry.Write(0, toWrite.TruncateStart(cacheEntry.Length));
              }
            }
            else
            {
              long spliceIndex1 = requestBegin() - cacheEntry.Begin;

              if ((cacheEntry.Length - spliceIndex1) <= toWrite.Length)
              {
                var (left, right) = cacheEntry.Split(spliceIndex1);

                right.Write(0, toWrite.TruncateStart(right.Length));

                Cache.RemoveAt(index);
                Cache.Insert(index++, left);
                Cache.Insert(index, right);
              }
              else
              {
                long spliceIndex2 = spliceIndex1 + toWrite.Length;

                var (left, center, right) = cacheEntry.Split(spliceIndex1, spliceIndex1 + toWrite.Length);

                center.Write(0, toWrite.TruncateStart(toWrite.Length));

                Cache.RemoveAt(index);
                Cache.Insert(index++, left);
                Cache.Insert(index++, center);
                Cache.Insert(index, right);
              }
            }
          }
        }
      }

      return Task.CompletedTask;
    }
  }

  private sealed class PathEqualityComparer(StoragePool pool) : IEqualityComparer<string[]>
  {
    public readonly StoragePool Pool = pool;

    public bool Equals(string[]? x, string[]? y) => (x == y) || (x != null && y != null && x.SequenceEqual(y));
    public int GetHashCode([DisallowNull] string[] obj) => HashCode.Combine(obj.Select((entry) => entry.GetHashCode()));
  }

  protected StoragePool(StoragePoolManager manager, StoragePoolResource resource) : base($"#{resource.Id}", manager)
  {
    Manager = manager;
    Resource = resource;

    TaskQueue = new();

    PathComparer = new(this);
    Handles = new(PathComparer);
    HandleBufferCache = new(PathComparer);
    HandleWaitQueue = new();
  }

  public readonly StoragePoolResource Resource;
  public readonly StoragePoolManager Manager;

  private readonly TaskQueue TaskQueue;

  private readonly PathEqualityComparer PathComparer;
  private readonly Dictionary<string[], FileHandle> Handles;
  private readonly Dictionary<string[], List<FileHandle.BufferCache>> HandleBufferCache;
  private readonly WaitQueue<(TaskCompletionSource<FileHandle> source, (UserAuthenticationContext authenticationContext, string[] path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken) args)> HandleWaitQueue;

  protected abstract Task<Information> InternalStat(string[] path, CancellationToken cancellationToken);
  protected abstract Task InternalDelete(string[] path, CancellationToken cancellationToken);
  protected abstract Task InternalMove(string[] fromPath, string[] toPath, CancellationToken cancellationToken);

  protected abstract Task<FileHandle> InternalOpen(UserAuthenticationContext authenticationContext, string[] path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken);

  protected abstract Task InternalCreateDirectory(string[] path, CancellationToken cancellationToken);
  protected abstract Task InternalRemoveDirectory(string[] path, CancellationToken cancellationToken);
  protected abstract Task<Information[]> InternalScanDirectory(string[] path, CancellationToken cancellationToken);

  protected abstract Task InternalCreateSymbolicLink(string[] path, string target, CancellationToken cancellationToken);
  protected abstract Task InternalReadSymbolicLink(string[] path, string target, CancellationToken cancellationToken);

  public Task<Information> Stat(string[] path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => InternalStat(path, cancellationToken), cancellationToken);
  public Task Delete(string[] path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => InternalDelete(path, cancellationToken), cancellationToken);
  public Task Move(string[] fromPath, string[] toPath, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => InternalMove(fromPath, toPath, cancellationToken), cancellationToken);

  public Task<FileHandle> Open(UserAuthenticationContext authenticationContext, string[] path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken) => TaskQueue.RunTask(async (cancellationToken) =>
  {
    TaskCompletionSource<FileHandle> source = new();

    await HandleWaitQueue.Enqueue((source, (authenticationContext, path, access, mode, cancellationToken)), cancellationToken);
    return await source.Task;
  }, cancellationToken);

  private List<FileHandle.BufferCache> AcquireHandleBufferList(string[] path)
  {
    if (!HandleBufferCache.TryGetValue(path, out var value))
    {
      HandleBufferCache.Add(path, value = []);
    }

    return value;
  }

  private async Task RunOpenQueue(CancellationToken serviceCancellationToken)
  {
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();

      var (source, (authenticationContext, path, access, mode, cancellationToken)) = await HandleWaitQueue.Dequeue(serviceCancellationToken);

      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, serviceCancellationToken);
      try
      {
        FileHandle handle;
        if (Handles.TryGetValue(path, out var value))
        {
          handle = value;
        }
        else
        {
          handle = await InternalOpen(authenticationContext, path, access, mode, linkedCancellationTokenSource.Token);

          handle.StateChanged += (_, state) =>
          {
            lock (Handles)
            {
              switch (state)
              {
                case ServiceState.Starting:
                case ServiceState.Started:
                  if (Handles.TryGetValue(path, out var _))
                  {
                    break;
                  }

                  Handles.Add(path, handle);
                  break;

                default:
                  Handles.Remove(path);
                  break;
              }
            }
          };

          await handle.Start();
        }

        source.SetResult(handle);
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
      finally
      {
        linkedCancellationTokenSource.Dispose();
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;

  protected override Task OnRun(CancellationToken cancellationToken) => Task.WhenAny(RunOpenQueue(cancellationToken), TaskQueue.Start(cancellationToken));

  protected override async Task OnStop(Exception? exception)
  {
    foreach (var (_, handle) in new Dictionary<string[], FileHandle>(Handles))
    {
      await handle.Stop();
    }
  }
}

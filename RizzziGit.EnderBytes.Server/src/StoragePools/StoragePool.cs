using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Collections.ObjectModel;

namespace RizzziGit.EnderBytes.StoragePools;

using Utilities;
using Buffer;
using Resources;
using Collections;
using System.Collections;

public abstract class StoragePool : Service
{
  public abstract class Exception : System.Exception
  {
    private static Exception Wrap(System.Exception innerException) => innerException is Exception exception
      ? exception
      : new InternalException(innerException);

    public static async Task<T> Wrap<T>(Task<T> task)
    {
      try
      {
        return await task;
      }
      catch (Exception exception)
      {
        throw Wrap(exception);
      }
    }

    public static async Task Wrap(Task task)
    {
      try
      {
        await task;
      }
      catch (Exception exception)
      {
        throw Wrap(exception);
      }
    }

    private Exception(string? message, System.Exception? innerException) : base(message, innerException) { }

    public class NoSuchFileOrFolder(System.Exception? innerException = null) : Exception("No such file or directory.", innerException);
    public class FileOrFolderExists(System.Exception? innerException = null) : Exception("File or folder already exists", innerException);
    public class InternalException(System.Exception? innerException = null) : Exception("Internal exception.", innerException);
    public class IsAFolder(System.Exception? innerException = null) : Exception("Path specified is a folder.", innerException);
    public class NotAFolder(System.Exception? innerException = null) : Exception("Path specified is not a folder.", innerException);
    public class AccessDenied(System.Exception? innerException = null) : Exception("Permission denied.", innerException);
  }

  public abstract record Information(string Name, long? AccessTime, long? TrashTime)
  {
    public sealed record File(string Name, long Size, long? AccessTime, long? TrashTime) : Information(Name, AccessTime, TrashTime);
    public sealed record Folder(string Name, long? AccessTime, long? TrashTime) : Information(Name, AccessTime, TrashTime);
    public sealed record SymbolicLink(string Name, long? AccessTime, long? TrashTime) : Information(Name, AccessTime, TrashTime);
    public sealed record Root(long? AccessTime, long? TrashTime) : Information("/", AccessTime, TrashTime);
  }

  public sealed class Path : IEnumerable<string>
  {
    public Path(StoragePool pool, params string[] path)
    {
      Pool = pool;

      {
        List<string> sanitized = [];
        foreach (string pathEntry in path)
        {
          if (
            (pathEntry.Length == 0) ||
            (pathEntry == ".")
          )
          {
            continue;
          }
          else if (pathEntry == "..")
          {
            if (sanitized.Count == 0)
            {
              throw new ArgumentException("Invalid path.", nameof(path));
            }

            sanitized.RemoveAt(sanitized.Count - 1);
            continue;
          }

          sanitized.Add(pathEntry);
        }

        InternalPath = [.. sanitized];
        sanitized.Clear();
      }
    }

    private readonly StoragePool Pool;
    private readonly string[] InternalPath;

    public string this[int index] => InternalPath[index];
    public int Length => InternalPath.Length;

    public bool IsInsideOf(Path other)
    {
      if (Length <= other.Length)
      {
        return false;
      }

      for (int index = 0; index < other.Length; index++)
      {
        if (string.Equals(
          this[index],
          other[index],
          Pool.Resource.Flags.HasFlag(
            StoragePoolFlags.IgnoreCase)
              ? StringComparison.OrdinalIgnoreCase
              : StringComparison.Ordinal
          )
        )
        {
          return false;
        }
      }

      return true;
    }

    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      else if (obj is null || obj is not Path)
      {
        return false;
      }

      Path path = (Path)obj;
      if (path.InternalPath.Length != InternalPath.Length)
      {
        return false;
      }

      if (path.Pool != Pool)
      {
        throw new ArgumentException("Cannot compare paths from different storage pools.");
      }

      for (int index = 0; index < Length; index++)
      {
        if (string.Equals(
          this[index],
          path[index],
          Pool.Resource.Flags.HasFlag(
            StoragePoolFlags.IgnoreCase)
              ? StringComparison.InvariantCultureIgnoreCase
              : StringComparison.InvariantCulture
          )
        )
        {
          return false;
        }
      }

      return true;
    }

    public override int GetHashCode()
    {
      HashCode hashCode = new();

      foreach (string pathEntry in InternalPath)
      {
        hashCode.Add(pathEntry);
      }

      return hashCode.ToHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<string> GetEnumerator()
    {
      foreach (string pathEntry in InternalPath)
      {
        yield return pathEntry;
      }
    }
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

    protected FileHandle(StoragePool pool, Path path, FileAccess access) : base(string.Join("/", path), pool)
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
    protected override Task OnStop(System.Exception? exception) => Sync();

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
      if (!Access.HasFlag(FileAccess.Read))
      {
        throw new InvalidOperationException("Read flag is not present.");
      }

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
      if (!Access.HasFlag(FileAccess.Write))
      {
        throw new InvalidOperationException("Write flag is not present.");
      }

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

      Size = long.Max(Position += buffer.Length, Size);
      return Task.CompletedTask;
    }
  }

  private sealed class PathEqualityComparer(StoragePool pool) : IEqualityComparer<string[]>
  {
    public readonly StoragePool Pool = pool;

    public bool Equals(string[]? x, string[]? y) => (x == y) || (x != null && y != null && x.SequenceEqual(y));
    public int GetHashCode([DisallowNull] string[] obj) => HashCode.Combine(obj.Select((entry) => entry.GetHashCode()));
  }

  public sealed class Context(UserAuthenticationContext authentication, UserKeyResource key)
  {
    public readonly UserAuthenticationContext Authentication = authentication;
    public readonly UserKeyResource Key = key;

    public UserResource User => Authentication.User;
    public bool IsValid => Authentication.IsValid;

    private UserKeyResource.Transformer? Transformer;
    public UserKeyResource.Transformer GetTransformer() => Transformer ??= Authentication.GetTransformer(Key);
  }

  protected StoragePool(StoragePoolManager manager, StoragePoolResource resource) : base($"#{resource.Id}", manager)
  {
    Manager = manager;
    Resource = resource;

    TaskQueue = new();

    Handles = [];
    HandleBufferCache = [];
    HandleWaitQueue = new();
  }

  public readonly StoragePoolResource Resource;
  public readonly StoragePoolManager Manager;

  private readonly TaskQueue TaskQueue;

  private readonly Dictionary<Path, FileHandle> Handles;
  private readonly Dictionary<Path, List<FileHandle.BufferCache>> HandleBufferCache;
  private readonly WaitQueue<(TaskCompletionSource<FileHandle> source, (Context authenticationContext, Path path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken) args)> HandleWaitQueue;

  protected abstract Task<Information> InternalStat(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task InternalDelete(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task InternalMove(Context context, string[] fromPath, string[] toPath, CancellationToken cancellationToken);
  protected abstract Task InternalCopy(Context context, string[] sourcePath, string[] destinationPath, CancellationToken cancellationToken);

  protected abstract Task<FileHandle> InternalOpen(Context context, Path path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken);

  protected abstract Task InternalCreateDirectory(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task InternalRemoveDirectory(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task<Information[]> InternalScanDirectory(Context context, Path path, CancellationToken cancellationToken);

  protected abstract Task InternalCreateSymbolicLink(Context context, Path path, string[] target, CancellationToken cancellationToken);
  protected abstract Task<string[]> InternalReadSymbolicLink(Context context, Path path, CancellationToken cancellationToken);

  protected abstract Task<long> InternalTrash(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task InternalRestore(Context context, long trashedFileId, string[]? newPath, CancellationToken cancellationToken);

  public Task<Information> Stat(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalStat(context, path, cancellationToken)), cancellationToken);
  public Task Delete(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalDelete(context, path, cancellationToken)), cancellationToken);
  public Task Move(Context context, string[] fromPath, string[] toPath, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalMove(context, fromPath, toPath, cancellationToken)), cancellationToken);
  public Task Copy(Context context, string[] sourcePath, string[] destinationPath, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalCopy(context, sourcePath, destinationPath, cancellationToken)), cancellationToken);

  public Task<FileHandle> Open(Context context, Path path, FileHandle.FileAccess access, FileHandle.FileMode mode, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Exception.Wrap(run());

    async Task<FileHandle> run()
    {
      TaskCompletionSource<FileHandle> source = new();
      await HandleWaitQueue.Enqueue((source, (context, path, access, mode, cancellationToken)), cancellationToken);
      return await source.Task;
    }
  }, cancellationToken);

  public Task CreateDirectory(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalCreateDirectory(context, path, cancellationToken)), cancellationToken);
  public Task RemoveDirectory(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalRemoveDirectory(context, path, cancellationToken)), cancellationToken);
  public Task<Information[]> ScanDirectory(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalScanDirectory(context, path, cancellationToken)), cancellationToken);

  public Task CreateSymbolicLink(Context context, Path path, string[] target, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalCreateSymbolicLink(context, path, target, cancellationToken)), cancellationToken);
  public Task<string[]> ReadSymbolicLink(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalReadSymbolicLink(context, path, cancellationToken)), cancellationToken);

  public Task<long> Trash(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalTrash(context, path, cancellationToken)), cancellationToken);
  public Task Restore(Context context, long trashedFileId, string[]? newPath, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) => Exception.Wrap(InternalRestore(context, trashedFileId, newPath, cancellationToken)), cancellationToken);

  private List<FileHandle.BufferCache> AcquireHandleBufferList(Path path)
  {
    if (!HandleBufferCache.TryGetValue(path, out List<FileHandle.BufferCache>? list))
    {
      HandleBufferCache.Add(path, list = []);
    }

    return list;
  }

  private async Task RunOpenQueue(CancellationToken serviceCancellationToken)
  {
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();

      var (source, (context, path, access, mode, cancellationToken)) = await HandleWaitQueue.Dequeue(serviceCancellationToken);

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
          handle = await InternalOpen(context, path, access, mode, linkedCancellationTokenSource.Token);

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

  protected override async Task OnStop(System.Exception? exception)
  {
    List<Task> tasks = [];

    lock (Handles)
    {
      foreach (var (_, handle) in Handles)
      {
        tasks.Add(handle.Stop());
      }
    }

    await Task.WhenAll(tasks);
  }
}

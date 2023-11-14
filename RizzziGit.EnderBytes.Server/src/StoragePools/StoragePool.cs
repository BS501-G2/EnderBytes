namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Connections;
using Utilities;
using System.Reflection;
using RizzziGit.Collections;

public abstract class StoragePoolException : Exception;
public sealed class InvalidOperationException : StoragePoolException;
public sealed class PathNotFoundException : StoragePoolException;
public sealed class PathFileExistsException : StoragePoolException;
public sealed class ResourceInUseException : StoragePoolException;
public sealed class MissingKeyException : StoragePoolException;
public sealed class NotADirectoryException : StoragePoolException;
public sealed class IsADirectoryException : StoragePoolException;
public sealed class DeletedException : StoragePoolException;

public interface IStoragePool : ILifetime
{
  public bool MarkedForDeletion { get; set; }
}

public abstract class StoragePool<F> : Lifetime, IStoragePool
  where F : StoragePool<F>.FileHandle
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

  private class FileHandleBufferBlock(Buffer? buffer, long begin, bool toSync, long lastAccessTime, Func<FileHandleBufferBlock, Task> syncCallback)
  {
    public Buffer? Buffer = buffer;
    public long LastAccessTime = lastAccessTime;
    public long Begin = begin;
    public long End => Begin + Buffer?.Length ?? Begin;
    public long Length => Buffer?.Length ?? 0;
    public bool ToSync = toSync;
    public async Task Sync()
    {
      await syncCallback(this);
      ToSync = false;
    }

    public (FileHandleBufferBlock left, FileHandleBufferBlock right) Split(long index) => (
      new(Buffer!.Slice(0, index), Begin, ToSync, LastAccessTime, syncCallback),
      new(Buffer!.Slice(index, End), Begin + index, ToSync, LastAccessTime, syncCallback)
    );

    public (FileHandleBufferBlock left, FileHandleBufferBlock center, FileHandleBufferBlock right) Split(long index1, long index2) => (
      new(Buffer!.Slice(0, index1), Begin, ToSync, LastAccessTime, syncCallback),
      new(Buffer!.Slice(index1, index2), Begin + index1, ToSync, LastAccessTime, syncCallback),
      new(Buffer!.Slice(index2, End), Begin + index2, ToSync, LastAccessTime, syncCallback)
    );
  }

  public abstract class FileHandle : Lifetime
  {
    public FileHandle(StoragePool<F> storagePool, string[] path, FileAccess access, FileMode mode) : base($"/{string.Join("/", path)}")
    {
      Memory = storagePool.GetHandleMemory(this);
      StoragePool = storagePool;
      Access = access;
      Mode = mode;
    }

    private readonly List<FileHandleBufferBlock> Memory;

    public long Position { get; private set; }
    public long Size { get; private set; }
    public long CachedSize
    {
      get
      {
        long size = 0;
        foreach (FileHandleBufferBlock block in Memory)
        {
          size += block.Length;
        }

        return size;
      }
    }

    public readonly StoragePool<F> StoragePool;
    public readonly FileAccess Access;
    public readonly FileMode Mode;

    protected abstract Task<Buffer> InternalRead(long position, long length, CancellationToken cancellationToken);
    protected abstract Task InternalWrite(long position, Buffer buffer, CancellationToken cancellationToken);
    protected abstract Task<Information.File> InternalGetInfo(CancellationToken cancellationToken);
    protected abstract Task InternalTruncate(long size, CancellationToken cancellationToken);
    protected abstract Task InternalClose();

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      Information.File info = await RunTask(InternalGetInfo, cancellationToken);
      Size = info.Size;

      try
      {
        await base.OnRun(cancellationToken);
      }
      finally
      {
        foreach (FileHandleBufferBlock block in Memory)
        {
          if (!block.ToSync)
          {
            continue;
          }

          _ = block.Sync();
        }

        await RunTask(async (cancellationToken) => await InternalClose(), cancellationToken);
      }
    }

    private Task Flush(long minimumSize) => RunTask(() =>
    {
      long remaining = long.Min(minimumSize, CachedSize);

      List<FileHandleBufferBlock> sorted = [.. Memory];
      sorted.Sort((a, b) => Comparer<long>.Default.Compare(a.LastAccessTime, b.LastAccessTime));
      foreach (FileHandleBufferBlock block in sorted)
      {
        if (remaining == 0)
        {
          break;
        }

        _ = block.Sync();

        remaining = long.Max(0, remaining - block.Length);
        sorted.Remove(block);
      }
    }, GetCancellationToken());

    private static long GetTimestampNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private Task Sync(FileHandleBufferBlock block) => RunTask(async (cancellationToken) =>
    {
      if (block.ToSync)
      {
        await InternalWrite(block.Begin, block.Buffer!, cancellationToken);
      }
    }, GetCancellationToken());

    public Task<Buffer> Read(long length, CancellationToken cancellationToken) => RunTask(async (cancellationToken) =>
    {
      long requestBegin = Position;
      long requestEnd = Position + length > Size ? Size - Position : length;

      List<Task<Buffer>> toRead = [];
      lock (Memory)
      {
        for (int index = 0; index < Memory.Count && requestBegin < requestEnd; index++)
        {
          FileHandleBufferBlock block = Memory.ElementAt(index);

          if (requestBegin >= block.End)
          {
            continue;
          }

          if (requestBegin < block.Begin)
          {
            long internalReadLength = long.Min(block.Begin - requestBegin, requestEnd - requestBegin);
            Task<Buffer> readTask = InternalRead(requestBegin, internalReadLength, cancellationToken);
            FileHandleBufferBlock newBlock = new(null, requestBegin, false, GetTimestampNow(), Sync);

            _ = readTask.ContinueWith(async (task) => requestBegin += (newBlock.Buffer = await task).Length);
            toRead.Add(readTask);
            Memory.Insert(index++, newBlock);
          }
          else
          {
            long sliceBegin = requestBegin - block.Begin;
            long sliceEnd = sliceBegin + long.Min(requestEnd - requestBegin, block.Length - sliceBegin);

            Buffer sliced = block.Buffer!.Slice(sliceBegin, sliceEnd);
            toRead.Add(Task.FromResult(sliced));

            requestBegin += sliceEnd - sliceBegin;
          }
        }

        if (requestBegin < requestEnd)
        {
          Task<Buffer> read = InternalRead(requestBegin, requestEnd - requestBegin, cancellationToken);
          FileHandleBufferBlock newBlock = new(null, requestBegin, false, StoragePool<F>.FileHandle.GetTimestampNow(), Sync);

          requestBegin = requestEnd;

          _ = read.ContinueWith(async (task) => newBlock.Buffer = await task);
          Memory.Add(newBlock);
        }
      }

      Position = requestEnd;
      return Buffer.Concat(await Task.WhenAll(toRead));
    }, cancellationToken);

    public Task Write(Buffer buffer, CancellationToken cancellationToken) => RunTask(() =>
    {
      long requestBegin = Position;

      Buffer toWrite = buffer.Clone();
      lock (Memory)
      {
        for (int index = 0; index < Memory.Count && toWrite.Length != 0; index++)
        {
          FileHandleBufferBlock block = Memory.ElementAt(index);

          if (requestBegin >= block.End)
          {
            continue;
          }

          if (requestBegin < block.Begin)
          {
            Buffer spliced = toWrite.TruncateStart(block.Begin - requestBegin);
            FileHandleBufferBlock newBlock = new(spliced, requestBegin, true, GetTimestampNow(), Sync);

            Memory.Insert(index++, newBlock);
            requestBegin += spliced.Length;
          }
          else if (block.ToSync)
          {
            long spliceIndex = requestBegin - block.Begin;
            long spliceLength = long.Min(block.Length - spliceIndex, toWrite.Length);

            block.Buffer!.Write(spliceIndex, toWrite.TruncateStart(spliceLength));
            requestBegin += spliceLength;
          }
          else
          {
            if (block.Begin == requestBegin)
            {
              if (block.Length > toWrite.Length)
              {
                var (left, right) = block.Split(toWrite.Length);

                left.Buffer = toWrite.TruncateStart(toWrite.Length);
                left.ToSync = true;

                Memory.RemoveAt(index);
                Memory.Insert(index++, left);
                Memory.Insert(index, right);

                requestBegin += left.Length;
              }
              else
              {
                block.Buffer = toWrite.TruncateStart(block.Length);
                block.ToSync = true;

                requestBegin += block.Length;
              }
            }
            else
            {
              long spliceIndex1 = requestBegin - block.Begin;

              if (block.Length <= toWrite.Length)
              {
                var (left, right) = block.Split(spliceIndex1);

                right.Buffer = toWrite.TruncateStart(block.Length);
                right.ToSync = true;

                Memory.RemoveAt(index);
                Memory.Insert(index++, left);
                Memory.Insert(index, right);

                requestBegin += right.Length;
              }
              else
              {
                var (left, center, right) = block.Split(spliceIndex1, spliceIndex1 + toWrite.Length);

                center.Buffer = toWrite.TruncateStart(toWrite.Length);
                center.ToSync = true;

                Memory.RemoveAt(index);
                Memory.Insert(index++, left);
                Memory.Insert(index++, center);
                Memory.Insert(index, right);

                requestBegin += center.Length;
              }
            }
          }
        }

        if (toWrite.Length != 0)
        {
          FileHandleBufferBlock newBlock = new(toWrite.TruncateStart(toWrite.Length), requestBegin, true, StoragePool<F>.FileHandle.GetTimestampNow(), Sync);

          Memory.Add(newBlock);
          requestBegin += newBlock.Length;
        }
      }
    }, cancellationToken);
  }

  public abstract record Information(
    string[] Path,
    long CreateTime,
    long UpdateTime
  )
  {
    public record File(
      string[] Path,
      long Size,
      long CreateTime,
      long UpdateTime
    ) : Information(Path, CreateTime, UpdateTime);

    public record Folder(
      string[] Path,
      long CreateTime,
      long UpdateTime
    ) : Information(Path, CreateTime, UpdateTime);

    public record SymbolicLink(
      string[] Path,
      long CreateTime,
      long UpdateTime
    );
  }

  protected StoragePool(StoragePoolManager manager, StoragePoolResource storagePool, StoragePoolType type, string name) : base(name)
  {
    if (storagePool.Type != type)
    {
      throw new InvalidOperationException();
    }

    Manager = manager;
    Resource = storagePool;
    Handles = [];

    MarkedForDeletion = false;

    manager.Logger.Subscribe(Logger);
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  private readonly List<FileHandle> Handles;
  private readonly Dictionary<FileHandle, List<FileHandleBufferBlock>> HandleMemory;

  private List<FileHandleBufferBlock> GetHandleMemory(FileHandle fileHandle)
  {
    {
      if (HandleMemory.TryGetValue(fileHandle, out var memory))
      {
        return memory;
      }
    }

    {
      List<FileHandleBufferBlock> memory = [];
      HandleMemory.Add(fileHandle, memory);
      return memory;
    }
  }

  public bool MarkedForDeletion { get; set; }
  public long CacheSize
  {
    get
    {
      long size = 0;
      foreach (FileHandle handle in Handles)
      {
        size += handle.CachedSize;
      }

      return size;
    }
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return base.OnRun(cancellationToken);
  }

  protected abstract Task<F> InternalOpen(UserKeyResource userKey, byte[] hashCache, CancellationToken cancellationToken);

  public async Task<F> Open(UserKeyResource userKey, byte[] hashCache, CancellationToken cancellationToken)
  {
    F handle = await InternalOpen(userKey, hashCache, cancellationToken);

    handle.Stopped += (_, _) => Handles.Remove(handle);
    return handle;
  }
}

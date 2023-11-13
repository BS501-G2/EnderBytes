namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Connections;
using Utilities;
using System.Reflection;

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

  public abstract class FileHandle(string[] path, FileAccess access, FileMode mode) : Lifetime($"/{string.Join("/", path)}")
  {
    protected class BufferBlock(Buffer? buffer, long begin, bool pendingWrite, long lastAccessTime)
    {
      public Buffer? Buffer = buffer;
      public long LastAccessTime = lastAccessTime;
      public long Begin = begin;
      public long End => Begin + Buffer?.Length ?? Begin;
      public long Length => Buffer?.Length ?? 0;
      public bool PendingWrite = pendingWrite;

      public (BufferBlock left, BufferBlock right) Split(long index) => (
        new(Buffer!.Slice(0, index), Begin, PendingWrite, LastAccessTime),
        new(Buffer!.Slice(index, End), Begin + index, PendingWrite, LastAccessTime)
      );

      public (BufferBlock left, BufferBlock center, BufferBlock right) Split(long index1, long index2) => (
        new(Buffer!.Slice(0, index1), Begin, PendingWrite, LastAccessTime),
        new(Buffer!.Slice(index1, index2), Begin + index1, PendingWrite, LastAccessTime),
        new(Buffer!.Slice(index2, End), Begin + index2, PendingWrite, LastAccessTime)
      );
    }

    private readonly List<BufferBlock> Memory = [];

    public long Position { get; private set; }
    public long Size { get; private set; }
    public long CachedSize
    {
      get
      {
        long size = 0;
        foreach (BufferBlock block in Memory)
        {
          size += block.Length;
        }

        return size;
      }
    }

    public readonly FileAccess Access = access;
    public readonly FileMode Mode = mode;

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
        foreach (BufferBlock block in Memory)
        {
          if (!block.PendingWrite)
          {
            continue;
          }

          block.PendingWrite = false;
          _ = InternalWrite(Position, block.Buffer!.Clone(), cancellationToken);
        }

        await InternalClose();
      }
    }

    private Task Flush() => RunTask(async (cancellationToken) =>
    {
      List<Task> tasks = [];

      if (tasks.Count == 0)
      {
        return;
      }

      await Task.WhenAll(tasks);
    }, GetCancellationToken());

    private long GetTimestampNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public Task<Buffer> Read(long length, CancellationToken cancellationToken) => RunTask(async (cancellationToken) =>
    {
      long requestBegin = Position;
      long requestEnd = Position + length > Size ? Size - Position : length;

      List<Task<Buffer>> toRead = [];
      lock (Memory)
      {
        for (int index = 0; index < Memory.Count && requestBegin < requestEnd; index++)
        {
          BufferBlock block = Memory.ElementAt(index);

          if (requestBegin >= block.End)
          {
            continue;
          }

          if (requestBegin < block.Begin)
          {
            long internalReadLength = long.Min(block.Begin - requestBegin, requestEnd - requestBegin);
            Task<Buffer> readTask = InternalRead(requestBegin, internalReadLength, cancellationToken);
            BufferBlock newBlock = new(null, requestBegin, false, GetTimestampNow());

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
          BufferBlock newBlock = new(null, requestBegin, false, GetTimestampNow());

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
          BufferBlock block = Memory.ElementAt(index);

          if (requestBegin >= block.End)
          {
            continue;
          }

          if (requestBegin < block.Begin)
          {
            Buffer spliced = toWrite.TruncateStart(block.Begin - requestBegin);
            BufferBlock newBlock = new(spliced, requestBegin, true, GetTimestampNow());

            Memory.Insert(index++, newBlock);
            requestBegin += spliced.Length;
          }
          else if (block.PendingWrite)
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
                left.PendingWrite = true;

                Memory.RemoveAt(index);
                Memory.Insert(index++, left);
                Memory.Insert(index, right);

                requestBegin += left.Length;
              }
              else
              {
                block.Buffer = toWrite.TruncateStart(block.Length);
                block.PendingWrite = true;

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
                right.PendingWrite = true;

                Memory.RemoveAt(index);
                Memory.Insert(index++, left);
                Memory.Insert(index, right);

                requestBegin += right.Length;
              }
              else
              {
                var (left, center, right) = block.Split(spliceIndex1, spliceIndex1 + toWrite.Length);

                center.Buffer = toWrite.TruncateStart(toWrite.Length);
                center.PendingWrite = true;

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
          BufferBlock newBlock = new(toWrite.TruncateStart(toWrite.Length), requestBegin, true, GetTimestampNow());

          Memory.Add(newBlock);
          requestBegin += newBlock.Length;
        }
      }
    }, cancellationToken);

    public void ClearCache(long minimumSize)
    {
      long remaining = long.Min(minimumSize, CachedSize);

      List<BufferBlock> sorted = [..Memory];
      sorted.Sort((a, b) => Comparer<long>.Default.Compare(a.LastAccessTime, b.LastAccessTime));
      foreach (BufferBlock block in sorted)
      {
        if (remaining == 0)
        {
          break;
        }

        if (block.PendingWrite)
        {
          block.PendingWrite = false;
          _ = InternalWrite(block.Begin, block.Buffer!, GetCancellationToken());
        }

        remaining = long.Max(0, remaining - block.Length);
        sorted.Remove(block);
      }
    }
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

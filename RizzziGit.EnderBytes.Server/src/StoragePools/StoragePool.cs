namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Connections;
using Utilities;

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
    protected class BufferBlock(Buffer? buffer, long begin, bool pendingWrite)
    {
      public static BufferBlock Join(params BufferBlock[] blocks)
      {
        if (blocks.Length == 0)
        {
          throw new ArgumentException("Empty args.", nameof(blocks));
        }

        if (blocks.Length == 1)
        {
          return blocks[0];
        }

        Buffer joined = Buffer.Empty();

        long? lastIndex = null;
        foreach (BufferBlock block in blocks)
        {
          if (lastIndex != null && lastIndex != block.Begin)
          {
            throw new ArgumentException("Not continuous.", nameof(blocks));
          }

          if (block.Buffer == null)
          {
            throw new ArgumentException("All blocks must not be empty.", nameof(blocks));
          }

          joined.Append(block.Buffer);

          lastIndex = block.End;
        }

        return new(joined, blocks[0].Begin, blocks[0].PendingWrite);
      }

      public Buffer? Buffer = buffer;
      public long Begin = begin;
      public long End => Begin + Buffer?.Length ?? Begin;
      public long Length => Buffer?.Length ?? 0;
      public bool PendingWrite = pendingWrite;

      public (BufferBlock left, BufferBlock right) Split(long index) => (
        new(Buffer!.Slice(0, index), Begin, PendingWrite),
        new(Buffer!.Slice(index, End), Begin + index, PendingWrite)
      );

      public (BufferBlock left, BufferBlock center, BufferBlock right) Split(long index1, long index2) => (
        new(Buffer!.Slice(0, index1), Begin, PendingWrite),
        new(Buffer!.Slice(index1, index2), Begin + index1, PendingWrite),
        new (Buffer!.Slice(index2, End), Begin + index2, PendingWrite)
      );
    }

    private readonly List<BufferBlock> Memory = [];

    public long Position { get; private set; }
    public long Size
    {
      get
      {
        long size = 0;

        lock (Memory)
        {
          foreach (BufferBlock block in Memory)
          {
            size += block.Length;
          }
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

      try
      {
        await base.OnRun(cancellationToken);
      }
      finally
      {
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
            BufferBlock newBlock = new(null, requestBegin, false);

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
          BufferBlock newBlock = new(null, requestBegin, false);

          requestBegin = requestEnd;

          _ = read.ContinueWith(async (task) => newBlock.Buffer = await task);
          Memory.Add(newBlock);
        }
      }

      Position = requestEnd;
      return Buffer.Concat(await Task.WhenAll(toRead));
    }, cancellationToken);

    public Task Write(Buffer buffer, CancellationToken cancellationToken) => RunTask(async (cancellationToken) =>
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
            BufferBlock newBlock = new(spliced, requestBegin, true);

            Memory.Insert(index++, newBlock);
            requestBegin += spliced.Length;
          }
          else if (block.PendingWrite)
          {
            long spliceIndex = requestBegin - block.Begin;

            block.Buffer!.Write(spliceIndex, toWrite.TruncateStart(long.Min(block.Length - spliceIndex, toWrite.Length)));
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
              }
              else
              {
                block.Buffer = toWrite.TruncateStart(block.Length);
                block.PendingWrite = true;
              }
            }
            else
            {

            }
          }
        }

        if (toWrite.Length != 0)
        {

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

    MarkedForDeletion = false;

    manager.Logger.Subscribe(Logger);
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  public bool MarkedForDeletion { get; set; }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return base.OnRun(cancellationToken);
  }

  public abstract Task<F> Open(UserKeyResource userKey, byte[] hashCache, CancellationToken cancellationToken);
}

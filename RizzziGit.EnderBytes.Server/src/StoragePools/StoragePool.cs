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
    protected record BufferBlock(Buffer Buffer, long Begin, long End, bool PendingWrite)
    {
      public BufferBlock(Buffer Buffer, long Begin, bool PendingWrite) : this(Buffer, Begin, Begin + Buffer.Length, PendingWrite) { }
    }

    private readonly List<BufferBlock> InternalCache = [];

    public long Position { get; private set; } = 0;
    public long Size { get; private set; } = 0;

    public readonly FileAccess Access = access;
    public readonly FileMode Mode = mode;

    protected abstract Task<Buffer> InternalRead(long position, long length, CancellationToken cancellationToken);
    protected abstract Task InternalWrite(long position, Buffer buffer, CancellationToken cancellationToken);
    protected abstract Task<Information.File> InternalGetInfo(CancellationToken cancellationToken);
    protected abstract Task InternalClose();

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      Information.File info = await InternalGetInfo(cancellationToken);
      Size = info.Size;

      try
      {
        await base.OnRun(cancellationToken);
      }
      finally
      {
        await InternalClose();
      }
    }

    public Task<Buffer> Read(long length, CancellationToken cancellationToken) => RunTask(async (cancellationToken) =>
    {
      List<Buffer> list = [];

      length = Position + length > Size ? Size - Position : length;

      long bytesRead = 0;
      long requestBegin() => Position + bytesRead;
      long requestEnd() => Position + length;

      while (bytesRead < length)
      {
        for (int index = 0; index < InternalCache.Count; index++)
        {
          BufferBlock block = InternalCache.ElementAt(index);

          if (block.Begin > requestBegin())
          {
            BufferBlock newBlock;
            {
              Buffer buffer = await InternalRead(requestBegin(), block.Begin - requestBegin(), cancellationToken);
              InternalCache.Insert(index, newBlock = new(buffer, requestBegin(), false));
              index++;
            }

            long toAdd = 0;

            list.Add(newBlock.Buffer);
            toAdd += newBlock.Buffer.Length;

            if (newBlock.End < requestEnd())
            {
              if (block.End > requestEnd())
              {
                Buffer sliced = block.Buffer.Slice(0, - (requestEnd() - block.End));
                toAdd += sliced.Length;
                list.Add(sliced);
              }
              else
              {
                toAdd += block.Buffer.Length;
                list.Add(block.Buffer);
              }
            }

            bytesRead += toAdd;
          }

          // if (requestBegin() < blockBegin())
          // {
          //   Buffer newBlock = await InternalRead(requestBegin(), blockBegin(), cancellationToken);
          //   long newBlockEnd() => requestBegin() + newBlock.Length;

          //   if (newBlockEnd() > requestBegin())
          //   {
          //     list.Add(newBlock.Slice(0, - (newBlockEnd() - requestBegin())));
          //     InternalCache.Insert(index, new(newBlock, requestBegin(), false));
          //   }
          // }
          // else
          // {
          //   if (requestBegin() > blockEnd())
          //   {
          //     continue;
          //   }
          // }
        }
      }

      Buffer output = Buffer.Concat(list);
      Position += output.Length;
      return output;
    }, cancellationToken);

    public Task Write(Buffer buffer, CancellationToken cancellationToken) => RunTask(async (cancellationToken) =>
    {

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

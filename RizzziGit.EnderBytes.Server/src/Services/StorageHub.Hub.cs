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
      public sealed class BufferCache(FileHandle handle, long begin, Buffer buffer, bool toWrite)
      {
        public readonly FileHandle Handle = handle;

        public long Begin { get; private set; } = begin;
        public long End => Begin + Buffer.Length;
        public long Length => Buffer.Length;
        public readonly Buffer Buffer = buffer;
        public bool ToWrite { get; private set; } = toWrite;

        public Buffer Read(long begin, long end) => Buffer.Read(begin - Begin, end - begin);
        public void Write(long begin, Buffer buffer)
        {
          Buffer.Write(begin - Begin, buffer);
          ToWrite = true;
        }
        public Buffer Slice(long begin, long end) => Buffer.Slice(begin - Begin, end - Begin);

        public (BufferCache left, BufferCache right) Split(long index) => (
          new(Handle, Begin + index, Buffer.Slice(index, Buffer.Length), ToWrite),
          new(Handle, Begin, Buffer.Slice(0, index), ToWrite)
        );

        public (BufferCache left, BufferCache center, BufferCache right) Split(long index1, long index2) => (
          new(Handle, Begin, Buffer.Slice(0, index1), ToWrite),
          new(Handle, Begin + index1, Buffer.Slice(index1, index2), ToWrite),
          new(Handle, Begin + index2, Buffer.Slice(index2, Buffer.Length), ToWrite)
        );

        public void Prepend(Buffer buffer)
        {
          Buffer.Prepend(buffer);
          Begin -= buffer.Length;
        }

        public void Append(Buffer buffer)
        {
          Buffer.Append(buffer);
        }
      }

      private readonly KeyGeneratorService.Transformer.Key Transformer = transformer;
      private readonly List<BufferCache> Cache = [];

      public readonly Hub Hub = hub;
      public readonly long FileId = fileId;
      public readonly long SnapshotId = snapshotId;

      protected abstract long Internal_Position { get; }
      protected abstract long Internal_Size { get; }

      public long Position { get; private set; }
      public long Size { get; private set; }

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

      private void StoreCache(int index, int spliceCount, params BufferCache[] buffers)
      {
        if (spliceCount > 0)
        {
          Cache.RemoveRange(index, spliceCount);
        }

        Cache.InsertRange(index, buffers);
      }

      public Task<Buffer> Read(long size) => RunTask(async (_) =>
      {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 0);

        Buffer output = Buffer.Empty();

        long requestBegin() => Position + output.Length;
        long requestEnd() => long.Min(Position + size, Size);

        if (requestBegin() == requestEnd())
        {
          return output;
        }

        for (int index = 0; (requestBegin() < requestEnd()) && (index < Cache.Count); index++)
        {
          BufferCache cache = Cache[index];

          if (requestEnd() <= cache.Begin)
          {
            Buffer buffer = await Internal_Read(requestBegin(), requestEnd());

            if (cache.ToWrite || (cache.Begin != requestEnd()))
            {
              StoreCache(index++, 0, [new(this, requestBegin(), buffer, false)]);
            }
            else
            {
              StoreCache(index, 1, [new(this, requestBegin(), Buffer.Concat(buffer, cache.Buffer), false)]);
            }

            output.Append(buffer);
          }
          else if (requestBegin() <= cache.Begin)
          {
            if (requestBegin() < cache.Begin)
            {
              Buffer buffer = await Internal_Read(requestBegin(), cache.Begin);

              if (cache.ToWrite)
              {
                StoreCache(index++, 0, [new(this, requestBegin(), buffer, false)]);
              }
              else
              {
                StoreCache(index, 1, [new(this, requestBegin(), Buffer.Concat(buffer, cache.Buffer), false)]);
              }

              output.Append(buffer);
            }

            output.Append(cache.Slice(cache.Begin, long.Min(cache.End, requestEnd())));
          }
          else if (requestBegin() > cache.Begin)
          {
            output.Append(cache.Slice(requestBegin(), long.Min(cache.End, requestEnd())));
          }
        }

        if (requestBegin() < requestEnd())
        {
          Buffer buffer = await Internal_Read(requestBegin(), requestEnd());
          Cache.Add(new(this, requestBegin(), buffer, false));
        }

        Position = requestEnd();
        return output;
      });

      public Task Write(Buffer buffer) => RunTask(async (_) =>
      {
        Buffer remainingBytes = buffer.Clone();
        remainingBytes.CopyOnWrite = true;

        long requestBegin() => Position + (buffer.Length - remainingBytes.Length);
        long requestEnd() => requestBegin() + remainingBytes.Length;

        if (buffer.Length == 0)
        {
          return;
        }

        for (int index = 0; remainingBytes.Length > 0 && index < Cache.Count; index++)
        {
          BufferCache cache = Cache[index];

          if (requestEnd() <= cache.Begin)
          {
            if (!cache.ToWrite)
            {
              StoreCache(index++, 0, [new(this, requestBegin(), remainingBytes.TruncateStart(remainingBytes.Length), true)]);
            }
            else
            {
              StoreCache(index, 1, [new(this, requestBegin(), Buffer.Concat(remainingBytes.TruncateStart(remainingBytes.Length), cache.Buffer), true)]);
            }
          }
          else if (requestBegin() <= cache.Begin)
          {
            if (requestBegin() < cache.Begin)
            {
              if (!cache.ToWrite)
              {
                StoreCache(index++, 0, [new(this, requestBegin(), remainingBytes.TruncateStart(cache.Begin - requestBegin()), true)]);
              }
              else
              {
                StoreCache(index, 1, [cache = new(this, requestBegin(), Buffer.Concat(remainingBytes.TruncateStart(cache.Begin - requestBegin()), cache.Buffer), true)]);
              }
            }

            if (cache.ToWrite)
            {
              cache.Write(requestBegin(), remainingBytes.TruncateStart(long.Min(remainingBytes.Length, cache.Length)));
            }
            else if (requestEnd())
            {
            }
          }
        }

        Position = requestEnd();
      });

      public Task Seek(long position) => RunTask(async (_) =>
      {
        await Internal_SetSize(position);
      });

      public Task SetSize(long size) => RunTask(async (_) =>
      {
        await Internal_SetSize(size);
      });

      protected override Task OnRun(CancellationToken cancellationToken)
      {
        return base.OnRun(cancellationToken);
      }
    }

    public readonly StorageHubService Service = service;
    public readonly long HubId = hubId;
    public readonly KeyGeneratorService.Transformer.Key HubKey = hubKey;

    private readonly WeakDictionary<long, List<FileHandle.BufferCache>> FileHandleBufferCache = [];
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

namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Extensions;

public abstract partial class StoragePool
{
  public abstract partial class Handle
  {
    public abstract partial class File : Handle
    {
      [Flags]
      public enum Access : byte
      {
        Read = 1 << 0,
        Write = 1 << 1,

        ReadWrite = Read | Write
      }

      [Flags]
      public enum Mode : byte
      {
        TruncateToZero = 1 << 0,
        Append = 1 << 1,
        NewSnapshot = 1 << 2
      }

      protected File(StoragePool pool) : base(pool) { }

      public abstract IAsyncEnumerable<Snapshot> GetSnapshots(Context context, CancellationToken cancellationToken);
      public abstract Task<Snapshot> CreateSnapshot(Context context, Snapshot? baseSnapshot, CancellationToken cancellationToken);

      private readonly WeakKeyDictionary<Snapshot, List<Stream.BufferCache>> BufferCaches = new();
      private readonly Dictionary<Snapshot, Stream> Streams = [];

      private async Task<List<Stream.BufferCache>> GetCacheList(Snapshot snapshot)
      {
        lock (BufferCaches)
        {
          if (BufferCaches.TryGetValue(snapshot, out List<Stream.BufferCache>? value))
          {
            return value;
          }
        }

        Snapshot? parent = await snapshot.GetParentSnapshot();
        if (parent is not null)
        {
          List<Stream.BufferCache> parentCacheList = await GetCacheList(parent);
          lock (BufferCaches)
          {
            BufferCaches.Add(snapshot, new(parentCacheList));
            return parentCacheList;
          }
        }
        else
        {
          lock (BufferCaches)
          {
            List<Stream.BufferCache> list = [];
            BufferCaches.Add(snapshot, list);
            return list;
          }
        }
      }

      protected abstract Task<Stream> InternalOpen(Context context, Snapshot snapshot, Access access, CancellationToken cancellationToken);

      public async Task<Stream> Open(Context context, Snapshot snapshot, Access access, Mode mode, CancellationToken cancellationToken)
      {
        if (mode.HasFlag(Mode.NewSnapshot))
        {
          snapshot = await CreateSnapshot(context, snapshot, cancellationToken);
        }

        Stream stream = await InternalOpen(context, snapshot, access, cancellationToken);

        if (mode.HasFlag(Mode.TruncateToZero))
        {
          await stream.Truncate(context, 0);
        }

        if (mode.HasFlag(Mode.Append))
        {
          await stream.Seek(0, SeekOrigin.End);
        }

        return stream;
      }

      public async Task<Stream> Open(Context context, Access access, Mode mode, CancellationToken cancellationToken)
      {
        Snapshot? snapshot = null;
        await foreach (Snapshot entry in GetSnapshots(context, cancellationToken))
        {
          if ((snapshot?.CreateTime ?? 0) < entry.CreateTime)
          {
            snapshot = entry;
          }
        }

        if (snapshot == null)
        {
          snapshot = await CreateSnapshot(context, null, cancellationToken);

          return await Open(context, snapshot, access, mode.RemoveFlag(Mode.NewSnapshot), cancellationToken);
        }

        return await Open(context, snapshot, access, mode, cancellationToken);
      }
    }
  }
}

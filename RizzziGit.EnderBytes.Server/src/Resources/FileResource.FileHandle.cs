namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Services;

public sealed partial class FileResource
{
  public sealed record CrossTransactionalFileHandle(ResourceManager Manager, FileHandle Handle) : IDisposable
  {
    public StorageResource Storage => Handle.Storage;
    public FileResource File => Handle.File;
    public UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken => Handle.UserAuthenticationToken;
    public FileHandleFlags Flags => Handle.Flags;

    public long Position => Handle.Position;

    public void ThrowIfDisposed() => Handle.ThrowIfDisposed();

    public Task Seek(long newPosition, CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      return Manager.Service.Transact((transaction, cancellationToken) => Handle.Seek(transaction, newPosition, cancellationToken), cancellationToken);
    }

    public Task<long> GetSize(CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      return Manager.Service.Transact(Handle.GetSize, cancellationToken);
    }

    public Task<CompositeBuffer> Read(int size, CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      return Manager.Service.Transact((transaction, cancellationToken) => Handle.Read(transaction, size, cancellationToken), cancellationToken);
    }

    public Task Write(CompositeBuffer compositeBuffer, CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      return Manager.Service.Transact((transaction, cancellationToken) => Handle.Write(transaction, compositeBuffer, cancellationToken), cancellationToken);
    }

    public Task Truncate(long size, CancellationToken cancellationToken = default)
    {
      ThrowIfDisposed();

      return Manager.Service.Transact((transaction, cancellationToken) => Handle.Truncate(transaction, size, cancellationToken), cancellationToken);
    }

    public void Dispose() => Handle.Dispose();
  }

  public abstract record FileHandle(ResourceManager Manager, StorageResource Storage, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags) : IDisposable
  {
    public long Position => InternalGetPosition();

    protected abstract long InternalGetPosition();

    public abstract void ThrowIfDisposed();

    public abstract void Seek(ResourceService.Transaction transaction, long newPosition, CancellationToken cancellationToken = default);
    public abstract long GetSize(ResourceService.Transaction transaction, CancellationToken cancellationToken = default);

    public abstract CompositeBuffer Read(ResourceService.Transaction transaction, int size, CancellationToken cancellationToken = default);
    public abstract void Write(ResourceService.Transaction transaction, CompositeBuffer buffer, CancellationToken cancellationToken = default);
    public abstract void Truncate(ResourceService.Transaction transaction, long size, CancellationToken cancellationToken = default);
    public abstract void Dispose();

    public CrossTransactionalFileHandle CrossTransactional => new(Manager, this);
  }
}

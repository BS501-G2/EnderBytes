namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;

public sealed class BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : StoragePool(manager, resource)
{
  private sealed class BlobFileSnapshot(Handle.File file) : Handle.File.Snapshot(file)
  {
    public override long Id => throw new NotImplementedException();
    public override long? CreateTime => throw new NotImplementedException();
    public override long? UpdateTime => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();

    public override Task<Handle.File.Snapshot?> GetParentSnapshot()
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobFileStream(Handle.File file, Handle.File.Snapshot snapshot, Handle.File.Access access) : Handle.File.Stream(file, snapshot, access)
  {
    protected override Task InternalClose()
    {
      throw new NotImplementedException();
    }

    protected override Task<Buffer> InternalRead(long position, long size)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalTruncate(long size)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalWrite(long position, Buffer buffer)
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobFileHandle(StoragePool pool) : Handle.File(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    public override Task<Snapshot> CreateSnapshot(Snapshot? baseSnapshot, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override IAsyncEnumerable<Snapshot> GetSnapshots(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Stream> InternalOpen(Snapshot snapshot, Access access, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobFolderHandle(StoragePool pool) : Handle.Folder(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalScan(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobSymbolicLinkHandle(StoragePool pool) : Handle.SymbolicLink(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    public override Task<Path> GetTargetPath(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobRootHandle(StoragePool pool) : Handle.Root(pool)
  {
    public override long Id => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    protected override Task<Handle> InternalGetHandle(Path path, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalScan(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private BlobRootHandle? Root = null;
  protected override Task<Handle.Root> InternalGetRoot(CancellationToken cancellationToken)
  {
    return Task.FromResult<Handle.Root>(Root ??= new(this));
  }

  protected override Task InternalRun(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalStart(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalStop(Exception? exception)
  {
    throw new NotImplementedException();
  }
}

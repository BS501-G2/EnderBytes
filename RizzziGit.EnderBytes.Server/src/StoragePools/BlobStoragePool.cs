namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using RizzziGit.EnderBytes.Connections;

public sealed class BlobStoragePool : StoragePool
{
  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
  }

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

  private sealed class BlobFileHandle(Context context, StoragePool pool) : Handle.File(context, pool)
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

  private sealed class BlobFolderHandle(Context context, StoragePool pool) : Handle.Folder(context, pool)
  {
    public override long Id => throw new NotImplementedException();
    public override Path Path => throw new NotImplementedException();
    public override long? AccessTime => throw new NotImplementedException();
    public override long? TrashTime => throw new NotImplementedException();

    protected override Task<File> InternalCreateFile(string name, long preallocateLength, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<File> InternalCreateFile(string name, File copyFromFile, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder> InternalCreateFolder(string name, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder> InternalCreateFolder(string name, Folder copyFromFolder, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<SymbolicLink> InternalCreateSymbolicLink(string name, Path target, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<SymbolicLink> InternalCreateSymbolicLink(string name, SymbolicLink copyFromSymbolicLink, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Folder?> InternalGetParent(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalRemove(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalScan(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private sealed class BlobSymbolicLinkHandle(Context context, StoragePool pool) : Handle.SymbolicLink(context, pool)
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

  public new sealed class Context : StoragePool.Context
  {
    public Context(StoragePool pool, Connection connection) : base(pool, connection)
    {
    }

    protected override Task<Handle> InternalGetByPath(Path path, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task<Handle.Folder> InternalGetRoot(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override IAsyncEnumerable<Handle> InternalGetTrashed(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }

  private readonly Resources.BlobStorage.ResourceManager Resources;
  private Resources.BlobStorage.FileNodeResource.ResourceManager Nodes => Resources.Nodes;

  protected override Task InternalStart(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRun(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalStop(Exception? exception)
  {
    throw new NotImplementedException();
  }
}

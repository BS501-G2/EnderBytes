namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Collections;

public abstract class StoragePool : EnderBytesServer.StoragePool, IAsyncDisposable
{
  public abstract class FileHandle
  {
    public FileHandle(FileInfo info)
    {
      Info = info;

      Info.StoragePool.FileHandles.Add(Info, this);
    }

    ~FileHandle()
    {
      Info.StoragePool.FileHandles.Remove(Info);
    }

    public readonly FileInfo Info;

    public abstract long Position { get; }

    public abstract Task<Buffer> Read(int length, CancellationToken cancellationToken);
    public abstract Task Write(Buffer buffer, CancellationToken cancellationTokene);
    public abstract Task Seek(long position, SeekOrigin seekOrigin, CancellationToken cancellationToken);
    public abstract Task Close(CancellationToken cancellationToken);
  }

  public abstract class DirectoryHandle
  {
    public DirectoryHandle(DirectoryInfo info)
    {
      Info = info;

      Info.StoragePool.DirectoryHandles.Add(Info, this);
    }

    ~DirectoryHandle()
    {
      Info.StoragePool.DirectoryHandles.Remove(Info);
    }

    public readonly DirectoryInfo Info;

    public abstract int Position { get; }

    public abstract Task<string> Read(CancellationToken cancellationToken);
    public abstract Task Write(string name, CancellationToken cancellationToken);
    protected abstract Task HandleClose(CancellationToken cancellationToken);

    public async Task Close(CancellationToken cancellationToken)
    {
      await HandleClose(cancellationToken);
      Info.StoragePool.DirectoryHandles.Remove(Info);
    }
  }

  public abstract class Info
  {
    public const byte TYPE_FILE = 0;
    public const byte TYPE_DIRECTORY = 1;
    public const byte TYPE_SYMBOLIC_LINK = 3;

    protected Info(StoragePool storagePool, IEnumerable<string> path, uint id, byte type)
    {
      switch (this)
      {
        case FileInfo:
        case DirectoryInfo:
        case SymbolicLinkInfo: break;

        default: throw new InvalidOperationException("Unknown class cannot extend Info.");
      }

      StoragePool = storagePool;
      ID = id;
      Type = type;
      Path = path.ToArray();

      StoragePool.Infos.Add(ID, this);
    }

    ~Info()
    {
      StoragePool.Infos.Remove(ID);
    }

    public readonly StoragePool StoragePool;
    public readonly uint ID;
    public readonly byte Type;

    public IEnumerable<string> Path { get; private set; }
    public bool IsDeleted { get; private set; }

    public async Task<bool> Move(IEnumerable<string> path, CancellationToken cancellationToken)
    {
      if (await StoragePool.Move(this, path, cancellationToken))
      {
        Path = path.ToArray();
        return true;
      }

      return false;
    }

    public async Task Delete(CancellationToken cancellationToken)
    {
      if (this is FileInfo fileInfo && StoragePool.FileHandles.TryGetValue(fileInfo, out FileHandle? fileHandle))
      {
        await fileHandle.Close(cancellationToken);
      }
      else if (this is DirectoryInfo directoryInfo && StoragePool.DirectoryHandles.TryGetValue(directoryInfo, out DirectoryHandle? directoryHandle))
      {
        await directoryHandle.Close(cancellationToken);
      }

      await StoragePool.Delete(this, cancellationToken);
      IsDeleted = true;
      StoragePool.Infos.Remove(ID);
    }
  }

  public abstract class FileInfo : Info
  {
    protected FileInfo(StoragePool storagePool, IEnumerable<string> path, uint fileId) : base(storagePool, path, fileId, TYPE_FILE)
    {
    }

    public abstract long Size { get; }

    public Task<FileHandle> Open(CancellationToken cancellationToken) => StoragePool.OpenFileHandle(this, cancellationToken);
  }

  public abstract class DirectoryInfo : Info
  {
    protected DirectoryInfo(StoragePool storagePool, IEnumerable<string> path, uint fileId) : base(storagePool, path, fileId, TYPE_DIRECTORY)
    {
    }

    public abstract int Size { get; }

    public Task<DirectoryHandle> Open(CancellationToken cancellationToken) => StoragePool.OpenDirectoryHandle(this, cancellationToken);
  }

  public abstract class SymbolicLinkInfo : Info
  {
    protected SymbolicLinkInfo(StoragePool storagePool, IEnumerable<string> path, uint fileId) : base(storagePool, path, fileId, TYPE_SYMBOLIC_LINK)
    {
    }

    public abstract Task<bool> Set(IEnumerable<string> target);
    public abstract Task<IEnumerable<string>> Get();

    public async Task<Info?> GetInfo(CancellationToken cancellationToken) => await StoragePool.Get(Path, cancellationToken);
  }

  public StoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : base(server, resource)
  {
    if (requireType != resource.Type)
    {
      throw new ArgumentException("Invalid type.", nameof(resource));
    }

    Type = requireType;

    Infos = new();
    FileHandles = new();
    DirectoryHandles = new();
  }

  public readonly uint Type;
  private readonly WeakDictionary<uint, Info> Infos;
  private readonly WeakKeyDictionary<FileInfo, FileHandle> FileHandles;
  private readonly WeakKeyDictionary<DirectoryInfo, DirectoryHandle> DirectoryHandles;

  protected Info ResolveInfo(uint fileId, Func<Info?, Info> callback) => Infos.TryGetValue(fileId, out Info? value) ? callback(value) : callback(null);

  protected abstract Task<FileHandle> OpenFileHandle(FileInfo info, CancellationToken cancellationToken);
  protected abstract Task<DirectoryHandle> OpenDirectoryHandle(DirectoryInfo info, CancellationToken cancellationToken);

  protected abstract Task<bool> Move(Info info, IEnumerable<string> destinationPath, CancellationToken cancellationToken);
  protected abstract Task Delete(Info info, CancellationToken cancellationToken);

  public abstract Task<Info?> Get(IEnumerable<string> path, CancellationToken cancellationToken);

  public abstract bool IsDisposed { get; protected set; }
  public abstract ValueTask DisposeAsync();
}

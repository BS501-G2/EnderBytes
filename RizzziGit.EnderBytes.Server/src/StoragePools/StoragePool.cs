namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Collections;

public sealed class FileInfo(byte type, int flags)
{
  public readonly byte Type = type;
  public readonly int Flags = flags;
}

public abstract class StoragePool : EnderBytesServer.StoragePool
{
  public StoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : base(server, resource)
  {
    if (requireType != resource.Type)
    {
      throw new ArgumentException("Invalid type.", nameof(resource));
    }

    Type = requireType;
  }

  public readonly uint Type;

  public abstract Task<FileInfo> Stats(IEnumerable<string> path, CancellationToken cancellationToken);

  public abstract Task CreateFile(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task<uint> OpenFile(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task DeleteFile(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task CloseFileHandle(uint handle, CancellationToken cancellationToken);
  public abstract Task WriteToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task InsertToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task<Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken);

  public abstract Task CreateDirectory(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task RemoveDirectory(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task<IEnumerable<string>> ReadDirectory(IEnumerable<string> path, CancellationToken cancellationToken);
}

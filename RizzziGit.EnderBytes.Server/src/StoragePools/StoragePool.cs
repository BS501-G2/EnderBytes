namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;

public sealed class FileInfo(string name, byte type, int mode, ulong createTime, ulong modifiedTime, ulong accessTime)
{
  public readonly string Name = name;
  public readonly byte Type = type;
  public readonly int Mode = mode;
  public readonly ulong CreateTime = createTime;
  public readonly ulong ModifiedTime = modifiedTime;
  public readonly ulong AccessTime = accessTime;
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
  public abstract Task ChangeMode(IEnumerable<string> path, int mode, CancellationToken cancellationToken);
  public abstract Task ChangeOwnership(IEnumerable<string> path, UserResource userResource, CancellationToken cancellationToken);

  public abstract Task CreateFile(IEnumerable<string> path, int mode, UserResource owner, CancellationToken cancellationToken);
  public abstract Task<uint> OpenFile(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task DeleteFile(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task CloseFileHandle(uint handle, CancellationToken cancellationToken);
  public abstract Task WriteToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task InsertToFileHandle(uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task<Buffer> ReadFromFileHandle(uint handle, long count, CancellationToken cancellationToken);
  public abstract Task SeekFileHandle(uint handle, long offset, SeekOrigin seekOrigin, CancellationToken cancellationToken);

  public abstract Task CreateDirectory(IEnumerable<string> path, int mode, UserResource owner, CancellationToken cancellationToken);
  public abstract Task RemoveDirectory(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task<IEnumerable<FileInfo>> ReadDirectory(IEnumerable<string> path, CancellationToken cancellationToken);

  public abstract Task CreateSymbolicLink(IEnumerable<string> path, CancellationToken cancellationToken);
  public abstract Task<string> ReadSymbolicLink(IEnumerable<string> path, string target, CancellationToken cancellationToken);
}

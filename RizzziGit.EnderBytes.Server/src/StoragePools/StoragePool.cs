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
  public const byte STATUS_OK = 0;
  public const byte STATUS_EPERM = 1;
  public const byte STATUS_ENOENT = 2;
  public const byte STATUS_EIO = 3;
  public const byte STATUS_EAGAIN = 11;
  public const byte STATUS_EACCESS = 13;
  public const byte STATUS_EBUSY = 13;
  public const byte STATUS_EEXIST = 17;
  public const byte STATUS_ENOTDIR = 20;
  public const byte STATUS_EISDIR = 21;
  public const byte STATUS_EINVAL = 22;
  public const byte STATUS_EMFILE = 23;
  public const byte STATUS_EFBIG = 27;
  public const byte STATUS_ENOSPC = 28;
  public const byte STATUS_ESPIPE = 29;
  public const byte STATUS_EROFS = 30;
  public const byte STATUS_EPIPE = 32;
  public const byte STATUS_ENOTEMPTY = 39;
  public const byte STATUS_EWOULDBLOCK = 40;
  public const byte STATUS_EOPNOTSUP = 95;
  public const byte STATUS_ESTALE = 116;
  public const byte STATUS_EDQUOT = 122;

  protected const string TRASH_NAME = ".Trash";

  public StoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : base(server, resource)
  {
    if (requireType != resource.Type)
    {
      throw new ArgumentException("Invalid type.", nameof(resource));
    }

    Type = requireType;
  }

  public readonly uint Type;

  public abstract Task<(byte status, FileInfo? stats)> Stats(UserResource initiator, string[] path, CancellationToken cancellationToken);
  public abstract Task<byte> ChangeMode(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken);
  public abstract Task<byte> ChangeOwnership(UserResource initiator, string[] path, CancellationToken cancellationToken);

  public abstract Task<byte> CreateFile(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken);
  public abstract Task<(byte status, uint handle)> OpenFile(UserResource initiator, string[] path, CancellationToken cancellationToken);
  public abstract Task<byte> DeleteFile(UserResource initiator, string[] path, CancellationToken cancellationToken);
  public abstract Task<byte> CloseFileHandle(UserResource initiator, uint handle, CancellationToken cancellationToken);
  public abstract Task<byte> WriteToFileHandle(UserResource initiator, uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task<byte> InsertToFileHandle(UserResource initiator, uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task<(byte status, Buffer buffer)> ReadFromFileHandle(UserResource initiator, uint handle, long count, CancellationToken cancellationToken);
  public abstract Task<byte> SeekFileHandle(UserResource initiator, uint handle, long offset, SeekOrigin seekOrigin, CancellationToken cancellationToken);

  public abstract Task<byte> CreateDirectory(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken);
  public abstract Task<byte> RemoveDirectory(UserResource initiator, string[] path, CancellationToken cancellationToken);
  public abstract Task<(byte status, IAsyncEnumerable<FileInfo> entries)> ReadDirectory(UserResource initiator, string[] path, CancellationToken cancellationToken);

  public abstract Task<byte> CreateSymbolicLink(UserResource initiator, string[] path, string target, CancellationToken cancellationToken);
  public abstract Task<(byte status, string target)> ReadSymbolicLink(UserResource initiator, string[] path, string target, CancellationToken cancellationToken);

  public abstract Task Shutdown (CancellationToken cancellationToken);
  public abstract Task Startup (CancellationToken cancellationToken);
}

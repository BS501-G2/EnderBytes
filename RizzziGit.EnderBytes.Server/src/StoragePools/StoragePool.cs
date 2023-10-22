namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Connections;
using Utilities;

public abstract class StoragePool
{
  public abstract record Information(
    UserResource OwnerUser,
    string[] Path,
    long CreateTime,
    long UpdateTime
  );

  public record FileInformation(
    UserResource OwnerUser,
    string[] Path,
    long Size,
    long CreateTime,
    long UpdateTime
  ) : Information(OwnerUser, Path, CreateTime, UpdateTime);

  public record DirectoryInformation(
    UserResource OwnerUser,
    string[] Path,
    long CreateTime,
    long UpdateTime
  ) : Information(OwnerUser, Path, CreateTime, UpdateTime);

  public record SymbolicLinkInformation(
    UserResource OwnerUser,
    string[] Path,
    long CreateTime,
    long UpdateTime
  );

  protected StoragePool(StoragePoolManager manager, StoragePoolResource storagePool, StoragePoolType type)
  {
    if (storagePool.Type != type)
    {
      throw new InvalidOperationException("Invalid storage pool type.");
    }

    Manager = manager;
    Resource = storagePool;
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  public abstract Task ChangeOwner(string[] path, UserResource user, CancellationToken cancellationToken);
  public abstract Task<Information> Stat(string[] path, CancellationToken cancellationToken);
  public abstract Task Delete(string[] path, CancellationToken cancellationToken);
  public abstract Task FileCreate(string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task<uint> FileOpen(string[] path, CancellationToken cancellationToken);
  public abstract Task FileClose(uint handle, CancellationToken cancellationToken);
  public abstract Task<Buffer> FileRead(uint handle, long length, CancellationToken cancellationToken);
  public abstract Task FileWrite(uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task FileSeek(uint handle, long position, CancellationToken cancellationToken);
  public abstract Task<FileInformation> FileStat(uint handle, CancellationToken cancellationToken);
  public abstract Task DirectoryCreate(string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task DirectoryRemove(string[] path, CancellationToken cancellationToken);
  public abstract Task<uint> DirectoryOpen(string[] path, CancellationToken cancellationToken);
  public abstract Task DirectoryClose(uint handle, CancellationToken cancellationToken);
  public abstract Task<IEnumerable<DirectoryInformation>> DirectoryStat(string[] path, CancellationToken cancellationToken);
  public abstract Task<Information> DirectoryRead(uint handle, long length, CancellationToken cancellationToken);
  public abstract Task SymbolicLinkCreate(string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task<string> SymbolicLinkRead(string[] path, CancellationToken cancellationToken);
  public abstract Task<SymbolicLinkInformation> SymbolicLinkStat(string[] path, CancellationToken cancellationToken);
}

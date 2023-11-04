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

  public abstract Task<Information[]> ListTrash(Connection connection, CancellationToken cancellationToken);
  public abstract Task Trash(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task ChangeOwner(Connection connection, string[] path, UserResource user, CancellationToken cancellationToken);
  public abstract Task<Information> Stat(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task Delete(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task FileCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task<uint> FileOpen(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task FileClose(Connection connection, uint handle, CancellationToken cancellationToken);
  public abstract Task<Buffer> FileRead(Connection connection, uint handle, long length, CancellationToken cancellationToken);
  public abstract Task FileWrite(Connection connection, uint handle, Buffer buffer, CancellationToken cancellationToken);
  public abstract Task FileSeek(Connection connection, uint handle, long position, CancellationToken cancellationToken);
  public abstract Task<FileInformation> FileStat(Connection connection, uint handle, CancellationToken cancellationToken);
  public abstract Task DirectoryCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task DirectoryRemove(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task<uint> DirectoryOpen(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task DirectoryClose(Connection connection, uint handle, CancellationToken cancellationToken);
  public abstract Task<DirectoryInformation> DirectoryStat(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task<Information> DirectoryRead(Connection connection, uint handle, long length, CancellationToken cancellationToken);
  public abstract Task SymbolicLinkCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken);
  public abstract Task<string> SymbolicLinkRead(Connection connection, string[] path, CancellationToken cancellationToken);
  public abstract Task<SymbolicLinkInformation> SymbolicLinkStat(Connection connection, string[] path, CancellationToken cancellationToken);
}

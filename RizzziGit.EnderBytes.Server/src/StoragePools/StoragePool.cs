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
}

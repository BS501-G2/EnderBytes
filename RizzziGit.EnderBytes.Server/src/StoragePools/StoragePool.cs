namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Connections;
using Utilities;

public abstract class StoragePoolException : Exception;

public sealed class InvalidOperationException : StoragePoolException;
public sealed class PathNotFoundException : StoragePoolException;
public sealed class PathFileExistsException : StoragePoolException;
public sealed class ResourceInUseException : StoragePoolException;
public sealed class MissingKeyException : StoragePoolException;
public sealed class NotADirectoryException : StoragePoolException;
public sealed class IsADirectoryException : StoragePoolException;
public sealed class DeletedException : StoragePoolException;

public abstract class StoragePool : Lifetime
{
  public abstract class FileHandle
  {
  }

  public abstract record Information(
    UserResource OwnerUser,
    string[] Path,
    long CreateTime,
    long UpdateTime
  )
  {
    public record File(
      UserResource OwnerUser,
      string[] Path,
      long Size,
      long CreateTime,
      long UpdateTime
    ) : Information(OwnerUser, Path, CreateTime, UpdateTime);

    public record Directory(
      UserResource OwnerUser,
      string[] Path,
      long CreateTime,
      long UpdateTime
    ) : Information(OwnerUser, Path, CreateTime, UpdateTime);

    public record SymbolicLink(
      UserResource OwnerUser,
      string[] Path,
      long CreateTime,
      long UpdateTime
    );
  }

  protected StoragePool(StoragePoolManager manager, StoragePoolResource storagePool, StoragePoolType type, string name): base(name)
  {
    if (storagePool.Type != type)
    {
      throw new InvalidOperationException();
    }

    Manager = manager;
    Resource = storagePool;

    MarkedForDeletion = false;

    manager.Logger.Subscribe(Logger);
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  public bool MarkedForDeletion;

  public abstract Task FileCreate(UserAuthenticationResource userAuthentication, byte[] hashCache, string[] path, CancellationToken cancellationToken);
  public abstract Task<long> FileOpen(string[] path, FileAccess fileAccess, CancellationToken cancellationToken);
  public abstract Task<Buffer> FileRead(long handleId, long size, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken);
  public abstract Task FileWrite(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken);
  public abstract Task FileTruncate(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken);
}

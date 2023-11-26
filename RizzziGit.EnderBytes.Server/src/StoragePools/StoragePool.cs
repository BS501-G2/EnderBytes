using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Collections.ObjectModel;

namespace RizzziGit.EnderBytes.StoragePools;

using Utilities;
using Buffer;
using Resources;
using Collections;
using System.Collections;
using System.Threading;

public abstract class BlobStorageException : Exception
{
  private static BlobStorageException Wrap(Exception innerException) => innerException is BlobStorageException exception
    ? exception
    : new InternalException(innerException);

  public static async Task<T> Wrap<T>(Task<T> task)
  {
    try
    {
      return await task;
    }
    catch (BlobStorageException exception)
    {
      throw Wrap(exception);
    }
  }

  public static async Task Wrap(Task task)
  {
    try
    {
      await task;
    }
    catch (BlobStorageException exception)
    {
      throw Wrap(exception);
    }
  }

  private BlobStorageException(string? message, Exception? innerException) : base(message, innerException) { }

  public class NoSuchFileOrFolder(Exception? innerException = null) : BlobStorageException("No such file or directory.", innerException);
  public class FileOrFolderExists(Exception? innerException = null) : BlobStorageException("File or folder already exists", innerException);
  public class InternalException(Exception? innerException = null) : BlobStorageException("Internal exception.", innerException);
  public class IsAFolder(Exception? innerException = null) : BlobStorageException("Path specified is a folder.", innerException);
  public class NotAFolder(Exception? innerException = null) : BlobStorageException("Path specified is not a folder.", innerException);
  public class AccessDenied(Exception? innerException = null) : BlobStorageException("Permission denied.", innerException);
}

public abstract partial class StoragePool : Service
{
  protected StoragePool(StoragePoolManager manager, StoragePoolResource resource) : base($"#{resource.Id}", manager)
  {
    Manager = manager;
    Resource = resource;
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  protected abstract Task InternalStart(CancellationToken cancellationToken);
  protected abstract Task InternalRun(CancellationToken cancellationToken);
  protected abstract Task InternalStop(System.Exception? exception);

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await InternalStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await InternalRun(cancellationToken);
  }

  protected override async Task OnStop(System.Exception? exception)
  {
    await InternalStop(exception);
  }

  protected abstract Task<Root> InternalGetRoot(Context context, CancellationToken cancellationToken);
  protected abstract IAsyncEnumerable<Handle> InternalGetTrashed(Context context, CancellationToken cancellationToken);

  public async Task<Root> GetRoot(Context context, CancellationToken cancellationToken)
  {
    return await InternalGetRoot(context, cancellationToken);
  }

  public async Task<Handle[]> GetTrashed(Context context, CancellationToken cancellationToken)
  {
    List<Handle> list = [];

    await foreach (Handle handle in InternalGetTrashed(context, cancellationToken))
    {
      list.Add(handle);
    }

    return [.. list];
  }
}

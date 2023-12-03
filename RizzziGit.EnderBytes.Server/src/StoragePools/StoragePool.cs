namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Connections;
using Utilities;
using Buffer;
using MongoDB.Bson;

public abstract partial class StoragePool : Service
{
  [Flags]
  public enum Access : byte
  {
    Read = 1 << 0,
    Write = 1 << 1,
    Exclusive = 1 << 2,

    ReadWrite = Read | Write,
    ExclusiveReadWrite = Exclusive | ReadWrite
  }

  [Flags]
  public enum Mode : byte
  {
    TruncateToZero = 1 << 0,
    Append = 1 << 1,
    NewSnapshot = 1 << 2
  }

  protected StoragePool(StoragePoolManager manager, StoragePoolResource resource) : base($"#{resource.Id}", manager)
  {
    Manager = manager;
    Resource = resource;
    TaskQueue = new();
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  private readonly TaskQueue TaskQueue;

  protected abstract Task Internal_OnStart(CancellationToken cancellationToken);
  protected abstract Task Internal_OnRun(CancellationToken cancellationToken);
  protected abstract Task Internal_OnStop(System.Exception? exception);

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Internal_OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await await Task.WhenAny(
      OnRun(cancellationToken),
      TaskQueue.Start(cancellationToken)
    );
  }

  protected override async Task OnStop(System.Exception? exception)
  {
    TaskQueue.Dispose(exception);
    await Internal_OnStop(exception);
  }
}

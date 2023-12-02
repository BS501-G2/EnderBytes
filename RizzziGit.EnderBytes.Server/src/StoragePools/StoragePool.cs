namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Connections;
using Utilities;
using System.Threading.Tasks;
using System.Threading;

public abstract partial class StoragePool : Service
{
  protected StoragePool(StoragePoolManager manager, StoragePoolResource resource, Action onStart, Action onStop) : base($"#{resource.Id}", manager)
  {
    Manager = manager;
    Resource = resource;
    TaskQueue = new();

    Manager_OnStart = onStart;
    Manager_OnStop = onStop;
  }

  public readonly StoragePoolManager Manager;
  public readonly StoragePoolResource Resource;

  private readonly Action Manager_OnStart;
  private readonly Action Manager_OnStop;
  private readonly TaskQueue TaskQueue;

  protected abstract Context Internal_GetContext(Connection connection);
  protected abstract Task Internal_OnStart(CancellationToken cancellationToken);
  protected abstract Task Internal_OnRun(CancellationToken cancellationToken);
  protected abstract Task Internal_OnStop(System.Exception? exception);

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    Manager_OnStart();
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
    Manager_OnStop();
  }
}

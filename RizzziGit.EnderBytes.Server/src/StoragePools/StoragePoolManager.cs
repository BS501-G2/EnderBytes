namespace RizzziGit.EnderBytes.StoragePools;

using Collections;
using Resources;

public sealed class StoragePoolManager : Service
{
  private class StoragePoolInformation(StoragePool pool)
  {
    private static long GetTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private readonly StoragePool PoolBackingField = pool;

    public StoragePool Pool
    {
      get
      {
        LastAccess = GetTimestamp();

        return PoolBackingField;
      }
    }

    public long LastAccess = GetTimestamp();
  }

  public StoragePoolManager(Server server)
  {
    Server = server;
    Server.Resources.StoragePools.ResourceDeleted += (transaction, resource) =>
    {
      lock (Pools)
      {
        if (Pools.TryGetValue(resource, out StoragePoolInformation? value))
        {
          _ = value.Pool.Stop();
        }
      }
    };
  }

  public readonly Server Server;

  private readonly Dictionary<StoragePoolResource, StoragePoolInformation> Pools = [];
  private readonly WaitQueue<(TaskCompletionSource<StoragePool> source, StoragePoolResource resource)> WaitQueue = new();
  private async Task RunOpenQueue(CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var (source, resource) = await WaitQueue.Dequeue(cancellationToken);

      StoragePool pool;
      if (Pools.TryGetValue(resource, out var value))
      {
        pool = value.Pool;
      }
      else
      {
        pool = resource.Type switch
        {
          StoragePoolType.Blob => new BlobStoragePool(this, resource),

          _ => throw new InvalidOperationException("Invalid storage pool type.")
        };

        try
        {
          pool.StateChanged += (_, state) =>
          {
            lock (Pools)
            {
              switch (state)
              {
                case ServiceState.Starting:
                case ServiceState.Started:
                  if (Pools.TryGetValue(resource, out var _))
                  {
                    break;
                  }

                  Pools.Add(resource, new(pool));
                  break;

                default:
                  Pools.Remove(resource);
                  break;
              }
            }
          };

          await pool.Start();
          source.SetResult(pool);
        }
        catch (Exception exception)
        {
          await pool.Stop();
          source.SetException(exception);
        }
      }
    }
  }


  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;

  protected override Task OnRun(CancellationToken cancellationToken) => RunOpenQueue(cancellationToken);

  protected override async Task OnStop(Exception? exception)
  {
    List<Task> tasks = [];

    lock (Pools)
    {
      foreach (var (_, information) in Pools)
      {
        tasks.Add(information.Pool.Stop());
      }
    }

    await Task.WhenAll(tasks);
  }
}

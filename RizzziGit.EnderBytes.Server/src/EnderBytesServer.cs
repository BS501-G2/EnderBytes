namespace RizzziGit.EnderBytes;

using Collections;
using Resources;

public struct EnderBytesConfig()
{
  public int DefaultPasswordIterations = 1000;
  public long ObsolescenceTimeSpan = 1000L * 60 * 60 * 24 * 30;

  public string DatabaseDir = Path.Join(Environment.CurrentDirectory, ".db");
}

public sealed class EnderBytesServer
{
  public abstract class Context
  {
    private static ulong GenerateAndRegisterID(EnderBytes.Context context, EnderBytesServer server)
    {
      ulong id;
      do
      {
        id = (ulong)Random.Shared.NextInt64();
      }
      while (!server.Contexts.TryAdd(id, new(context)));

      return id;
    }

    protected Context(EnderBytesServer server, CancellationToken cancellationToken)
    {
      Server = server;
      CancellationToken = cancellationToken;

      if (this is EnderBytes.Context context)
      {
        ID = GenerateAndRegisterID(context, server);
      }
      else
      {
        throw new InvalidOperationException($"Must be inherited from {nameof(EnderBytes.Context)} class.");
      }
    }

    ~Context() => Server.Contexts.Remove(ID);

    public readonly EnderBytesServer Server;
    public readonly ulong ID;
    public readonly CancellationToken CancellationToken;
  }

  public abstract class ProtocolWrapper
  {
    private static ulong GenerateAndRegisterID(ProtocolWrappers.ProtocolWrapper protocolWrapper, EnderBytesServer server)
    {
      ulong id;

      do
      {
        id = (ulong)Random.Shared.NextInt64();
      }
      while (!server.ProtocolWrappers.TryAdd(id, new(protocolWrapper)));

      return id;
    }

    public ProtocolWrapper(EnderBytesServer server)
    {
      Server = server;

      if (this is ProtocolWrappers.ProtocolWrapper wrapper)
      {
        ID = GenerateAndRegisterID(wrapper, server);
      }
      else
      {
        throw new InvalidOperationException($"Must be inherited from {nameof(EnderBytes.ProtocolWrappers.ProtocolWrapper)} class.");
      }
    }

    ~ProtocolWrapper() => Server.ProtocolWrappers.Remove(ID);

    protected async Task<EnderBytes.Context> GetContext(CancellationToken cancellationToken)
    {
      TaskCompletionSource<EnderBytes.Context> source = new();
      (await Server.ListenQueue.Dequeue(cancellationToken)).SetResult((source, cancellationToken));
      return await source.Task;
    }

    public readonly EnderBytesServer Server;
    public readonly ulong ID;
  }

  public abstract class StoragePool
  {
    protected StoragePool(EnderBytesServer server, StoragePoolResource resource)
    {
      lock (server.StoragePools)
      {
        if (this is StoragePools.StoragePool storagePool)
        {
          if (server.StoragePools.TryGetValue(resource, out WeakReference<StoragePools.StoragePool>? weakRef))
          {
            if (weakRef.TryGetTarget(out StoragePools.StoragePool? target))
            {
              throw new InvalidOperationException($"Storage pool already open.");
            }
            else
            {
              weakRef.SetTarget(storagePool);
            }
          }
          else
          {
            server.StoragePools.Add(resource, new(storagePool));
          }
        }
        else
        {
          throw new InvalidOperationException($"Must inherit {nameof(EnderBytes.StoragePools.StoragePool)} class.");
        }
      }

      Server = server;
      Resource = resource;
    }

    ~StoragePool() => Server.StoragePools.Remove(Resource);

    public readonly EnderBytesServer Server;
    public readonly StoragePoolResource Resource;
  }

  public EnderBytesServer(EnderBytesConfig? config = null)
  {
    Logger = new("Server");
    Resources = new(this);
    Config = config ?? new();
  }

  public readonly MainResourceManager Resources;

  public readonly Logger Logger;
  public readonly EnderBytesConfig Config;

  private readonly Dictionary<ulong, WeakReference<ProtocolWrappers.ProtocolWrapper>> ProtocolWrappers = [];
  private readonly Dictionary<ulong, WeakReference<EnderBytes.Context>> Contexts = [];
  private readonly Dictionary<StoragePoolResource, WeakReference<StoragePools.StoragePool>> StoragePools = [];

  private readonly WaitQueue<TaskCompletionSource<(TaskCompletionSource<EnderBytes.Context> source, CancellationToken cancellationToken)>> ListenQueue = new(0);

  public Task RunTransaction(Database.Database.TransactionCallback callback, CancellationToken cancellationToken) => Resources.RunTransaction(callback, cancellationToken);
  public Task<T> RunTransaction<T>(Database.Database.TransactionCallback<T> callback, CancellationToken cancellationToken) => Resources.RunTransaction(callback, cancellationToken);

  public async Task Run(CancellationToken cancellationToken)
  {
    await Resources.Init(cancellationToken);
  }

  public async Task Listen(CancellationToken cancelationToken)
  {
    while (true)
    {
      cancelationToken.ThrowIfCancellationRequested();

      TaskCompletionSource<(TaskCompletionSource<EnderBytes.Context> source, CancellationToken cancellationToken)> source = new();
      await ListenQueue.Enqueue(source, cancelationToken);
      var (remoteSource, remoteCancellationToken) = await source.Task;

      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        cancelationToken,
        remoteCancellationToken
      );

      if (linkedCancellationTokenSource.Token.IsCancellationRequested)
      {
        remoteSource.SetCanceled(linkedCancellationTokenSource.Token);
        continue;
      }

      remoteSource.SetResult(new(this, linkedCancellationTokenSource.Token));
    }
  }
}

namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Collections;

public sealed class ConnectionManager(Server server) : Service("Connections")
{
  public readonly Server Server = server;

  private readonly WeakDictionary<long, Connection> Connections = [];
  private readonly WaitQueue<(TaskCompletionSource<Connection> connection, ConnectionType type)> WaitQueue = new(0);

  public async Task<Connection> GetConnection(ConnectionType type, CancellationToken cancellationToken)
  {
    TaskCompletionSource<Connection> source = new();

    await WaitQueue.Enqueue((source, type), cancellationToken);
    return await source.Task;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach ((TaskCompletionSource<Connection> source, ConnectionType type) in WaitQueue)
    {
      try
      {
        long id;
        lock (Connections)
        {
          do { id = Random.Shared.NextInt64(); } while (Connections.ContainsKey(id));

          Connection connection = type switch
          {
            ConnectionType.Basic => new Connection.Basic(this, id),
            ConnectionType.Advanced => new Connection.Advanced(this, id),
            ConnectionType.Internal => new Connection.Internal(this, id),

            _ => throw new InvalidOperationException("Unknown type.")
          };

          Connections.Add(id, connection);
          source.SetResult(connection);
        }
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task OnStop(Exception? exception)
  {
    throw new NotImplementedException();
  }
}

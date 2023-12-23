namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Services;

public sealed partial class ConnectionService(Server server) : Service("Connections", server)
{
  public readonly Server Server = server;

  private readonly WaitQueue<(TaskCompletionSource<Connection> source, Configuration configuration)> WaitQueue = new(0);

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await foreach ((TaskCompletionSource<Connection> source, Configuration configuration) in WaitQueue.WithCancellation(cancellationToken))
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        Connection connection = configuration switch
        {
          Configuration.Advanced advancedConfiguration => new Connection.Advanced(advancedConfiguration),
          Configuration.Basic basicConfiguration => new Connection.Basic(basicConfiguration),
          Configuration.Internal internalConfiguration => new Connection.Internal(internalConfiguration),

          _ => throw new InvalidOperationException("Unknown configuration type.")
        };

        connection.Start(cancellationToken);
        source.SetResult(connection);
      }
      catch (OperationCanceledException exception)
      {
        source.SetException(exception);
        throw;
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnStop(Exception? exception) => Task.CompletedTask;
}

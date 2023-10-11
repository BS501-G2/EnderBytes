namespace RizzziGit.EnderBytes;

using Collections;
using Connections;
using ProtocolWrappers;
using Resources;

public sealed class EnderBytesServer
{
  public EnderBytesServer(EnderBytesConfig? config = null)
  {
    Logger = new("Server");
    Resources = new(this);
    Config = config ?? new();
    Connections = new();
  }

  public readonly MainResourceManager Resources;

  private readonly WaitQueue<TaskCompletionSource<(TaskCompletionSource<Connection> source, bool isDashboard)>> Connections;

  public readonly Logger Logger;
  public readonly EnderBytesConfig Config;

  private async Task Listen(TaskFactory factory, CancellationToken cancellationToken)
  {
    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();
      TaskCompletionSource<(TaskCompletionSource<Connection> source, bool isDashboard)> source = new();
      await Connections.Enqueue(source, cancellationToken);

      CancellationTokenSource cancellationTokenSource = new();
      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
      try
      {
        var awaitedSource = await source.Task;
        Connection connection = awaitedSource.isDashboard ? new DashboardConnection(this) : new ClientConnection(this);
        Task task = await factory.StartNew(() => connection.RunHandler(linkedCancellationTokenSource.Token), cancellationToken);
        awaitedSource.source.SetResult(connection);
        await task;
      }
      finally
      {
        linkedCancellationTokenSource.Dispose();
        cancellationTokenSource.Dispose();
      }
    }
  }

  public async Task Run(CancellationToken cancellationToken)
  {
    await Resources.Init(cancellationToken);
    await Task.WhenAll(
      Listen(
        new(
          cancellationToken,
          TaskCreationOptions.LongRunning,
          TaskContinuationOptions.LongRunning,
          null
        ),
        cancellationToken
      ),
      ProtocolWrapper.RunProtocolWrappers(
        this,
        async (cancellationToken) =>
        {
          TaskCompletionSource<Connection> source = new();
          (await Connections.Dequeue(cancellationToken)).SetResult((source, false));

          return (ClientConnection)await source.Task;
        },
        cancellationToken
      )
    );
  }
}

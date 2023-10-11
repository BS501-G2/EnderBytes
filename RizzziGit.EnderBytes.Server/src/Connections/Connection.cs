namespace RizzziGit.EnderBytes.Connections;

using Collections;

public abstract class Connection(EnderBytesServer server)
{
  private readonly WaitQueue<(ConnectionCommand command, CancellationToken cancellationToken, TaskCompletionSource<Task<ConnectionResponse>> source)> WaitQueue = new();

  public readonly EnderBytesServer Server = server;

  public async Task<ConnectionResponse> ExecuteCommand(ConnectionCommand command, CancellationToken cancellationToken)
  {
    TaskCompletionSource<Task<ConnectionResponse>> source = new();
    await WaitQueue.Enqueue((command, cancellationToken, source), cancellationToken);

    return await await source.Task;
  }

  protected abstract Task<ConnectionResponse> HandleCommand(ConnectionCommand command, CancellationToken cancellationToken);

  protected async Task<ConnectionResponse> HandleCommand(ConnectionCommand.AuthenticateWithPassword command, CancellationToken cancellationToken)
  {
    return new(ConnectionResponse.CODE_INVALID_COMMAND);
  }

  public async Task RunHandler(CancellationToken handlerCancellationToken)
  {
    while (true)
    {
      handlerCancellationToken.ThrowIfCancellationRequested();

      var (command, cancellationToken, source) = await WaitQueue.Dequeue(handlerCancellationToken);
      try
      {
        source.SetResult(HandleCommand(command, cancellationToken));
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }
}

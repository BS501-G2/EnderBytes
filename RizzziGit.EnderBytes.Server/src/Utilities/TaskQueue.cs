namespace RizzziGit.EnderBytes.Utilities;

using Collections;

internal class TaskQueue : IDisposable
{
  private readonly WaitQueue<(Func<CancellationToken, Task> callback, CancellationToken cancellationToken, TaskCompletionSource taskCompletionSource)> WaitQueue = new();

  void IDisposable.Dispose() => WaitQueue.Dispose();
  public void Dispose(Exception? exception) => WaitQueue.Dispose(exception);

  public async Task RunTask(Func<CancellationToken, Task> callback, CancellationToken cancellationToken)
  {
    TaskCompletionSource taskCompletionSource = new();
    await WaitQueue.Enqueue((callback, cancellationToken, taskCompletionSource), cancellationToken);
    await taskCompletionSource.Task;
  }

  public async Task<T> RunTask<T>(Func<CancellationToken, Task<T>> callback, CancellationToken cancellationToken)
  {
    TaskCompletionSource<T> taskCompletionSource = new();
    try
    {
      await RunTask(async (cancellationToken) =>
      {
        taskCompletionSource.SetResult(await callback(cancellationToken));
      }, cancellationToken);
    }
    catch (Exception exception)
    {
      taskCompletionSource.SetException(exception);
    }

    return await taskCompletionSource.Task;
  }

  public async Task Start(CancellationToken cancellationToken)
  {
    while (true)
    {
      var (callback, remoteCancellationToken, taskCompletionSource) = await WaitQueue.Dequeue(cancellationToken);

      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        remoteCancellationToken,
        cancellationToken
      );

      try
      {
        await callback(linkedCancellationTokenSource.Token);
        taskCompletionSource.SetResult();
      }
      catch (Exception exception)
      {
        taskCompletionSource.SetException(exception);
      }
      finally
      {
        linkedCancellationTokenSource.Dispose();
      }
    }
  }
}

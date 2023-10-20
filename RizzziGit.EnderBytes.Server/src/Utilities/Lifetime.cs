namespace RizzziGit.EnderBytes.Utilities;

public abstract class Lifetime
{
  protected Lifetime()
  {
    TaskQueue = new();
  }

  ~Lifetime() => Stop();

  public event EventHandler? Started;
  public event EventHandler? Stopped;

  private CancellationTokenSource? Source;
  private readonly TaskQueue TaskQueue;

  public bool IsRunning => Source != null;

  public void Stop() => Source?.Cancel();

  public Task RunTask(Func<CancellationToken, Task> callback, CancellationToken cancellationToken) => TaskQueue!.RunTask(callback, cancellationToken);
  public Task<T> RunTask<T>(Func<CancellationToken, Task<T>> callback, CancellationToken cancellationToken) => TaskQueue!.RunTask(callback, cancellationToken);
  public Task RunTask(Action callback) => TaskQueue!.RunTask(callback);
  public Task<T> RunTask<T>(Func<T> callback) => TaskQueue!.RunTask(callback);

  protected abstract Task OnRun(CancellationToken cancellationToken);

  private async void Run(CancellationTokenSource cancellationTokenSource, CancellationTokenSource linkedCancellationTokenSource)
  {
    try
    {
      Started?.Invoke(this, new());
      await OnRun(cancellationTokenSource.Token);
    }
    finally
    {
      lock (this)
      {
        linkedCancellationTokenSource.Dispose();
        cancellationTokenSource.Dispose();
        
        Source = null;
      }
      Stopped?.Invoke(this, new());
    }
  }

  public void Start(CancellationToken cancellationToken)
  {
    lock (this)
    {
      if (Source != null)
      {
        throw new InvalidOperationException("Already running.");
      }

      Run(Source = new(), CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, Source.Token
      ));
    }
  }
}
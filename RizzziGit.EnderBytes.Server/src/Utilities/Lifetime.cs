namespace RizzziGit.EnderBytes.Utilities;

public interface ILifetime
{
  public void Start(CancellationToken cancellationToken);
  public void Stop();
}

public abstract class Lifetime : ILifetime
{
  protected Lifetime(string name)
  {
    Name = name;
    Logger = new(name);
    TaskQueue = new();
  }

  ~Lifetime() => Stop();

  public event EventHandler? Started;
  public event EventHandler? Stopped;

  public readonly string Name;
  public readonly Logger Logger;

  private CancellationTokenSource? Source;
  private readonly TaskQueue TaskQueue;

  public bool IsRunning => Source != null;
  public Exception? Exception = null;
  public CancellationToken GetCancellationToken() => Source!.Token;

  public virtual void Stop() => Source?.Cancel();

  public Task RunTask(Func<CancellationToken, Task> callback, CancellationToken? cancellationToken = null) => TaskQueue!.RunTask(callback, cancellationToken);
  public Task<T> RunTask<T>(Func<CancellationToken, Task<T>> callback, CancellationToken? cancellationToken = null) => TaskQueue!.RunTask(callback, cancellationToken);
  public Task RunTask(Action callback) => TaskQueue!.RunTask(callback);
  public Task<T> RunTask<T>(Func<T> callback) => TaskQueue!.RunTask(callback);

  protected virtual Task OnRun(CancellationToken cancellationToken) => Task.Delay(-1, cancellationToken);

  private async void Run(CancellationTokenSource cancellationTokenSource, CancellationTokenSource linkedCancellationTokenSource)
  {
    Logger.Log(LogLevel.Verbose, "Lifetime started.");
    try
    {
      Started?.Invoke(this, new());
      await await Task.WhenAny(
        OnRun(linkedCancellationTokenSource.Token),
        TaskQueue.Start(linkedCancellationTokenSource.Token)
      );
    }
    catch (OperationCanceledException) { }
    catch (Exception exception) { Exception = exception; }
    finally
    {
      lock (this)
      {
        linkedCancellationTokenSource.Dispose();
        cancellationTokenSource.Dispose();

        Source = null;
      }
      Logger.Log(LogLevel.Verbose, "Lifetime stopped.");
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

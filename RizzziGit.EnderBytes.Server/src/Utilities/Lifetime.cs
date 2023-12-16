namespace RizzziGit.EnderBytes.Utilities;

public interface ILifetime
{
  public void Start(CancellationToken cancellationToken);
  public void Stop();
}

public abstract class Lifetime : ILifetime
{
  protected Lifetime(string name, Lifetime lifetime) : this(name, lifetime.Logger) { }
  protected Lifetime(string name, Logger? logger = null)
  {
    Name = name;
    Logger = new(name);

    logger?.Subscribe(Logger);
  }

  ~Lifetime() => Stop();

  public event EventHandler? Started;
  public event EventHandler? Stopped;

  public readonly string Name;
  public readonly Logger Logger;

  private CancellationTokenSource? Source;
  private readonly TaskQueue TaskQueue = new();

  public bool IsRunning
  {
    get
    {
      lock (this)
      {
        return Source != null;
      }
    }
  }
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
    using (linkedCancellationTokenSource)
    {
      linkedCancellationTokenSource.Token.Register(() =>
      {
        lock (this)
        {
          Source = null;
        }
      });

      using (cancellationTokenSource)
      {
        Logger.Log(LogLevel.Verbose, "Lifetime started.");
        try
        {
          Started?.Invoke(this, new());
          await await Task.WhenAny(
            OnRun(linkedCancellationTokenSource.Token),
            TaskQueue.Start(linkedCancellationTokenSource.Token)
          );

          lock (this)
          {
            Source = null;
          }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Exception = exception; }
        finally
        {
          Logger.Log(LogLevel.Verbose, "Lifetime stopped.");
          Stopped?.Invoke(this, new());
        }
      }
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

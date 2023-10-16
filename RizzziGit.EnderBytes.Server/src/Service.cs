namespace RizzziGit.EnderBytes;

public enum ServiceState : byte
{
  Starting = 0b110,
  Started = 0b100,
  Stopping = 0b010,
  Stopped = 0b000,
  Crashed = 0b001
}

public class ServiceContext(Task task, CancellationTokenSource stopToken)
{
  public readonly Task Task = task;
  public readonly CancellationTokenSource CancellationTokenSource = stopToken;
}

public abstract class Service
{
  private static TaskFactory? Factory = null;

  public Service() : this(null) { }
  public Service(string? name)
  {
    Factory ??= new(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

    Name = name ?? GetType().Name;
    State = ServiceState.Stopped;
    Logger = new(Name);
  }

  public readonly Logger Logger;
  public readonly string Name;
  public ServiceState State;

  public event EventHandler<ServiceState>? StateChanged;
  private void SetState(ServiceState state)
  {
    if (State == state)
    {
      return;
    }

    Logger.Log(LogLevel.Verbose, $"State Changed: {State} -> {state}");
    State = state;
    StateChanged?.Invoke(this, state);
  }

  private ServiceContext? Context;

  public async Task Start()
  {
    if (Context?.Task.IsCompleted == false)
    {
      return;
    }

    Context = null;

    TaskCompletionSource serviceStartupSource = new();
    TaskCompletionSource<Task> serviceRunnerSource = new();
    TaskCompletionSource serviceRunnerStartTriggerSource = new();

    _ = Factory?.StartNew(() =>
    {
      Thread.CurrentThread.Name = Name;
      Task task = RunThread(serviceStartupSource, serviceRunnerStartTriggerSource.Task);
      serviceRunnerSource.SetResult(task);
      serviceRunnerStartTriggerSource.SetResult();

      task.ContinueWith((task) => { }).Wait();
    });

    Context = new(await serviceRunnerSource.Task, new());

    await serviceStartupSource.Task;
  }

  private async Task RunThread(TaskCompletionSource startSource, Task onStart)
  {
    await onStart;
    if (Context == null)
    {
      return;
    }

    await Run(Context, startSource);
  }

  private async Task Run(ServiceContext context, TaskCompletionSource? startSource)
  {
    context.CancellationTokenSource.Token.ThrowIfCancellationRequested();
    try
    {
      SetState(ServiceState.Starting);
      await OnStart(context.CancellationTokenSource.Token);

      SetState(ServiceState.Started);
      startSource?.SetResult();
    }
    catch (OperationCanceledException)
    {
      Logger.Log(LogLevel.Info, $"Startup was cancelled on {Name}.");
    }
    catch (Exception exception)
    {
      Logger.Log(LogLevel.Fatal, $"Fatal Startup Exception on {Name}: {exception.Message}\n{exception.StackTrace}");
      SetState(ServiceState.Crashed);
      startSource?.SetException(exception);

      throw;
    }

    context.CancellationTokenSource.Token.ThrowIfCancellationRequested();
    try
    {
      await OnRun(context.CancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
      Logger.Log(LogLevel.Info, $"Operation was cancelled on {Name}.");
    }
    catch (Exception exception)
    {
      Logger.Log(LogLevel.Fatal, $"Fatal Exception on {Name}: {exception.Message}\n{exception.StackTrace}");
      SetState(ServiceState.Stopping);

      try
      {
        await OnStop(exception);
      }
      catch (Exception stopException)
      {
        Logger.Log(LogLevel.Fatal, $"Fatal Exception on {Name}: {stopException.Message}\n{stopException.StackTrace}");
        SetState(ServiceState.Crashed);
        throw new AggregateException(exception, stopException);
      }

      SetState(ServiceState.Crashed);
      throw;
    }

    SetState(ServiceState.Stopping);
    try
    {
      await OnStop(null);
    }
    catch
    {
      SetState(ServiceState.Crashed);
      throw;
    }
    SetState(ServiceState.Stopped);
  }

  public async Task Join()
  {
    if (Context != null) { await Context.Task; }
  }

  public async Task Stop()
  {
    ServiceContext? context = Context;
    if ((context == null) || context.CancellationTokenSource.IsCancellationRequested || context.Task.IsCompleted)
    {
      return;
    }

    try { context.CancellationTokenSource.Cancel(); } catch { }
    try { await context.Task; } catch { }
  }

  protected abstract Task OnStart(CancellationToken cancellationToken);
  protected abstract Task OnRun(CancellationToken cancellationToken);
  protected abstract Task OnStop(Exception? exception);
}

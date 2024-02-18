using System.Runtime.ExceptionServices;

namespace RizzziGit.EnderBytes.Utilities;

public static class TaskExtensions
{
  public static T WaitSync<T>(this Task<T> task, CancellationToken cancellationToken = default)
  {
    try { task.Wait(cancellationToken); }
    catch (AggregateException exception) { ExceptionDispatchInfo.Capture(exception.InnerException!).Throw(); throw; }

    return task.Result;
  }

  public static void WaitSync(this Task task, CancellationToken cancellationToken = default)
  {
    try { task.Wait(cancellationToken); }
    catch (AggregateException exception) { ExceptionDispatchInfo.Capture(exception.InnerException!).Throw(); throw; }
  }
}

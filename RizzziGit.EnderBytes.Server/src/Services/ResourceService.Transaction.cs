using Microsoft.Data.Sqlite;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Logging;

using WaitQueue = Framework.Collections.WaitQueue<(TaskCompletionSource Source, ResourceService.TransactionHandler Handler, CancellationToken CancellationToken)>;

public sealed partial class ResourceService
{
  public delegate void TransactionHandler(Transaction transaction);
  public delegate T TransactionHandler<T>(Transaction transaction);
  public delegate IEnumerable<T> TransactionEnumerableHandler<T>(Transaction transaction);
  public delegate void TransactionFailureHandler();

  public sealed record Transaction(Scope Scope, Action<TransactionFailureHandler> RegisterOnFailureHandler);

  private readonly WeakDictionary<Scope, WaitQueue> TransactionQueues = [];
  private WaitQueue GetTransactionWaitQueue(Scope scope)
  {
    lock (TransactionQueues)
    {
      if (!TransactionQueues.TryGetValue(scope, out var transactionQueue))
      {
        TransactionQueues.Add(scope, transactionQueue = new(0));
      }

      return transactionQueue;
    }
  }

  public Task<T> Transact<T>(ResourceManager resourceManager, TransactionHandler<T> handler, CancellationToken cancellationToken = default) => Transact(resourceManager.Scope, handler, cancellationToken);
  public Task Transact(ResourceManager resourceManager, TransactionHandler handler, CancellationToken cancellationToken = default) => Transact(resourceManager.Scope, handler, cancellationToken);

  public async IAsyncEnumerable<T> Transact<T>(Scope scope, TransactionEnumerableHandler<T> handler, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    WaitQueue<TaskCompletionSource<StrongBox<T>?>> waitQueue = new();

    _ = Transact(scope, async (transaction) =>
    {
      try
      {
        foreach (T item in handler(transaction))
        {
          (await waitQueue.Dequeue(cancellationToken)).SetResult(new(item));
        }

        (await waitQueue.Dequeue(cancellationToken)).SetResult(null);
      }
      catch (Exception exception)
      {
        (await waitQueue.Dequeue(cancellationToken)).SetException(exception);
      }
    }, cancellationToken);

    while (true)
    {
      TaskCompletionSource<StrongBox<T>?> source = new();
      await waitQueue.Enqueue(source, cancellationToken);
      StrongBox<T>? strongBox = await source.Task;

      if (strongBox == null)
      {
        yield break;
      }

      yield return strongBox.Value!;
    }
  }

  public async Task<T> Transact<T>(Scope scope, TransactionHandler<T> handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await Transact(scope, (transaction) => source.SetResult(handler(transaction)), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public async Task Transact(Scope scope, TransactionHandler handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource source = new();
    await GetTransactionWaitQueue(scope).Enqueue((source, handler, cancellationToken), cancellationToken);
    await source.Task;
  }

  private async Task RunTransactionQueue(Scope scope, CancellationToken serviceCancellationToken)
  {
    Logger.Log(LogLevel.Info, $"Transaction queue is running for database {scope}.");
    try
    {
      await foreach (var (source, handler, cancellationToken) in GetTransactionWaitQueue(scope).WithCancellation(serviceCancellationToken))
      {
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceCancellationToken, cancellationToken);
        Logger.Log(LogLevel.Debug, "Transaction begin.");
        using SqliteTransaction dbTransaction = GetDatabase(scope).BeginTransaction();

        List<TransactionFailureHandler> failureHandlers = [];
        Transaction transaction = new(scope, failureHandlers.Add);

        try
        {
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();
          handler(transaction);
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();

          Logger.Log(LogLevel.Debug, "Transaction commit.");
          dbTransaction.Commit();
        }
        catch (Exception exception)
        {
          Logger.Log(LogLevel.Warn, $"Transaction rollback due to exception: [{exception.GetType().Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}");
          dbTransaction.Rollback();

          foreach (TransactionFailureHandler failureHandler in failureHandlers.Reverse<TransactionFailureHandler>())
          {
            failureHandler();
          }

          source.SetException(exception);
          continue;
        }

        source.SetResult();
      }

    }
    catch (OperationCanceledException) { }
    catch
    {
      Logger.Log(LogLevel.Info, $"Transaction queue for database {scope} has crashed.");

      throw;
    }

    Logger.Log(LogLevel.Info, $"Transaction queue for database {scope} has stopped.");
  }
}

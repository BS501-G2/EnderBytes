using System.Data.SQLite;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Logging;
using RizzziGit.EnderBytes.Utilities;
using WaitQueue = Framework.Collections.WaitQueue<(TaskCompletionSource Source, ResourceService.TransactionHandler Handler, CancellationToken CancellationToken)>;

public sealed partial class ResourceService
{
  public delegate void TransactionHandler(Transaction transaction, ResourceService resourceService, CancellationToken cancellationToken);
  public delegate T TransactionHandler<T>(Transaction transaction, ResourceService resourceService, CancellationToken cancellationToken);
  public delegate IEnumerable<T> TransactionEnumeratorHandler<T>(Transaction transaction, ResourceService resourceService, CancellationToken cancellationToken);
  public delegate void TransactionFailureHandler();

  private long NextTransactionId;
  public sealed record Transaction(long Id, Action<TransactionFailureHandler> RegisterOnFailureHandler, CancellationToken CancellationToken);

  private WaitQueue? TransactionQueue;

  public async IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using WaitQueue<TaskCompletionSource<StrongBox<T>?>> waitQueue = new();

    _ = Transact((transaction, resourceService, cancellationToken) =>
    {
      try
      {
        foreach (T item in handler(transaction, resourceService, cancellationToken))
        {
          waitQueue.Dequeue(cancellationToken).WaitSync(cancellationToken).SetResult(new(item));
        }

        waitQueue.Dequeue(cancellationToken).WaitSync(cancellationToken).SetResult(null);
      }
      catch (Exception exception)
      {
        waitQueue.Dequeue(cancellationToken).WaitSync(cancellationToken).SetException(exception);
        throw;
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

  public async Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await Transact((transaction, resourceService, cancellationToken) => source.SetResult(handler(transaction, resourceService, cancellationToken)), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public async Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource source = new();

    await TransactionQueue!.Enqueue((source, handler, cancellationToken), cancellationToken);
    await source.Task;
  }

  private async Task RunTransactionQueue(SQLiteConnection connection, CancellationToken serviceCancellationToken)
  {
    Logger.Log(LogLevel.Info, $"Transaction queue is running for database.");
    try
    {
      try
      {
        await foreach (var (source, handler, cancellationToken) in (TransactionQueue = new()).WithCancellation(serviceCancellationToken))
        {
          using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceCancellationToken, cancellationToken);
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();

          using SQLiteTransaction dbTransaction = connection.BeginTransaction();

          List<TransactionFailureHandler> failureHandlers = [];
          Transaction transaction = new(NextTransactionId++, failureHandlers.Add, linkedCancellationTokenSource.Token);
          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction begin.");

          try
          {
            linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();
            handler(transaction, this, linkedCancellationTokenSource.Token);
            linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();

            Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction commit.");
            dbTransaction.Commit();
          }
          catch (Exception exception)
          {
            Logger.Log(LogLevel.Warn, $"[Transaction #{transaction.Id}] Transaction rollback due to exception: [{exception.GetType().Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}");
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
        Logger.Log(LogLevel.Info, $"Transaction queue for database has crashed.");

        throw;
      }
    }
    finally
    {
      TransactionQueue = null;
    }

    Logger.Log(LogLevel.Info, $"Transaction queue for database has stopped.");
  }
}

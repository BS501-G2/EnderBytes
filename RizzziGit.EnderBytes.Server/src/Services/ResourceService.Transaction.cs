using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Logging;

using WaitQueue = Framework.Collections.WaitQueue<(TaskCompletionSource Source, ResourceService.TransactionHandler Handler, CancellationToken CancellationToken)>;

public sealed partial class ResourceService
{
  public delegate void TransactionHandler(Transaction transaction);
  public delegate T TransactionHandler<T>(Transaction transaction);
  public delegate void TransactionFailureHandler();

  public sealed record Transaction(Scope Scope, Action<TransactionFailureHandler> RegisterOnFailureHandler, SqliteTransaction SqliteTransaction);

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

  public async Task<T> Transact<T>(Scope scope, TransactionHandler<T> transactionHandler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await Transact(scope, (transaction) => source.SetResult(transactionHandler(transaction)), cancellationToken);
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
        using SqliteTransaction dbTransaction = GetDatabase(scope).BeginTransaction();

        List<TransactionFailureHandler> failureHandlers = [];
        Transaction transaction = new(scope, failureHandlers.Add, dbTransaction);

        try
        {
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();
          handler(transaction);
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();
          dbTransaction.Commit();
        }
        catch (Exception exception)
        {
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

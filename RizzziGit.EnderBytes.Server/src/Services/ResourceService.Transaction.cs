using System.Runtime.CompilerServices;
using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Logging;

using Utilities;

using WaitQueue = Framework.Collections.WaitQueue<(TaskCompletionSource Source, ResourceService.AsyncTransactionHandler Handler, CancellationToken CancellationToken)>;

public sealed partial class ResourceService
{
  public delegate Task AsyncTransactionHandler(Transaction transaction, CancellationToken cancellationToken);
  public delegate void TransactionHandler(Transaction transaction, CancellationToken cancellationToken);
  public delegate Task<T> AsyncTransactionHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate T TransactionHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate IAsyncEnumerable<T> AsyncTransactionEnumeratorHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate IEnumerable<T> TransactionEnumeratorHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate Task AsyncTransactionFailureHandler();
  public delegate void TransactionFailureHandler();

  private long NextTransactionId;
  public sealed record Transaction(DbConnection Connection, long Id, Action<TransactionFailureHandler> RegisterOnFailureHandler, ResourceService ResoruceService, CancellationToken CancellationToken);

  public IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler, CancellationToken cancellationToken) => EnumeratedTransact((transaction, cancellationToken) => handler(transaction, cancellationToken).ToAsyncEnumerable(), cancellationToken);

  public Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default) => Transact<T>((transaction, cancellationToken) => Task.FromResult(handler(transaction, cancellationToken)), cancellationToken);

  public Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default) => Transact((AsyncTransactionHandler)((transaction, cancellationToken) =>
  {
    handler(transaction, cancellationToken);
    return Task.CompletedTask;
  }), cancellationToken);

  public async IAsyncEnumerable<T> EnumeratedTransact<T>(AsyncTransactionEnumeratorHandler<T> handler, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using WaitQueue<TaskCompletionSource<StrongBox<T>?>> waitQueue = new();

    _ = Transact((AsyncTransactionHandler)(async (transaction, cancellationToken) =>
    {
      try
      {
        await foreach (T item in handler(transaction, cancellationToken))
        {
          (await waitQueue.Dequeue(cancellationToken)).SetResult(new(item));
        }

        (await waitQueue.Dequeue(cancellationToken)).SetResult(null);
      }
      catch (Exception exception)
      {
        (await waitQueue.Dequeue(cancellationToken)).SetException(exception);
        throw;
      }
    }), cancellationToken);

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

  public async Task<T> Transact<T>(AsyncTransactionHandler<T> handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await Transact((AsyncTransactionHandler)(async (transaction, cancellationToken) => source.SetResult(await handler(transaction, cancellationToken))), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public Task Transact(AsyncTransactionHandler handler, CancellationToken cancellationToken = default)
  {
    return Database!.Run(async (connection, cancellationToken) =>
    {
      using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(GetCancellationToken(), cancellationToken);
      using DbTransaction dbTransaction = connection.BeginTransaction();

      List<TransactionFailureHandler> failureHandlers = [];
      Transaction transaction = new(connection, NextTransactionId++, failureHandlers.Add, this, linkedCancellationTokenSource.Token);
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction begin.");

      try
      {
        await handler(transaction, linkedCancellationTokenSource.Token);
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

        throw;
      }
    }, cancellationToken);
  }
}

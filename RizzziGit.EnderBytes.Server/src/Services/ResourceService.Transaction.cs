using System.Runtime.CompilerServices;
using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;
using Commons.Logging;

using Utilities;
using Core;

public sealed partial class ResourceService
{
  public delegate void TransactionHandler(Transaction transaction, CancellationToken cancellationToken);
  public delegate T TransactionHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate IEnumerable<T> TransactionEnumeratorHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate void TransactionFailureHandler();

  private long NextTransactionId;
  public sealed record Transaction(DbConnection Connection, long Id, ResourceService ResourceService, CancellationToken CancellationToken)
  {
    public Server Server => ResourceService.Server;

    public T GetManager<T>() where T : ResourceManager => ResourceService.GetManager<T>();
  }

  public async IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using WaitQueue<TaskCompletionSource<StrongBox<T>?>> waitQueue = new();

    _ = Transact((transaction, cancellationToken) =>
    {
      try
      {
        foreach (T item in handler(transaction, cancellationToken))
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
      await Transact((transaction, cancellationToken) => source.SetResult(handler(transaction, cancellationToken)), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default)
  {
    return Database!.Run((connection, cancellationToken) =>
    {
      using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(GetCancellationToken(), cancellationToken);
      using DbTransaction dbTransaction = connection.BeginTransaction();

      Transaction transaction = new(connection, NextTransactionId++, this, linkedCancellationTokenSource.Token);
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction begin.");

      while (true)
      {
        try
        {
          handler(transaction, linkedCancellationTokenSource.Token);
          linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();

          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction commit.");

          dbTransaction.Commit();
          break;
        }
        catch (Exception exception)
        {
          if (exception.Message.Contains("deadlock"))
          {
            continue;
          }

          Logger.Log(LogLevel.Warn, $"[Transaction #{transaction.Id}] Transaction rollback due to exception: [{exception.GetType().Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}");

          dbTransaction.Rollback();
          throw;
        }
      }
    }, cancellationToken);
  }
}

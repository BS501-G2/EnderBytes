using System.Runtime.CompilerServices;
using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;
using Commons.Logging;

using Utilities;
using Core;
using System.Runtime.ExceptionServices;

public sealed partial class ResourceService
{
  public delegate Task TransactionHandler(Transaction transaction, CancellationToken cancellationToken);
  public delegate Task<T> TransactionHandler<T>(Transaction transaction, CancellationToken cancellationToken);
  public delegate IAsyncEnumerable<T> TransactionEnumeratorHandler<T>(Transaction transaction, CancellationToken cancellationToken);

  private long NextTransactionId;
  public sealed record Transaction(DbConnection Connection, long Id, ResourceService ResourceService, CancellationToken CancellationToken)
  {
    public Server Server => ResourceService.Server;

    public T GetManager<T>() where T : ResourceManager => ResourceService.GetManager<T>();
  }

  public async IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    WaitQueue<StrongBox<T>?> waitQueue = new(0);
    ExceptionDispatchInfo? exceptionDispatchInfo = null;

    _ = Transact(async (transaction, cancellationToken) =>
    {
      try
      {
        await foreach (T item in handler(transaction, cancellationToken))
        {
          await waitQueue.Enqueue(new(item), cancellationToken);
        }
      }
      catch (Exception exception)
      {
        exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
      }

      await waitQueue.Enqueue(null, cancellationToken);
    }, cancellationToken);

    await foreach (StrongBox<T>? item in waitQueue)
    {
      if (item == null)
      {
        break;
      }

      yield return item.Value!;
    }

    if (exceptionDispatchInfo != null)
    {
      exceptionDispatchInfo.Throw();
    }
  }

  public async Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await Transact(async (transaction, cancellationToken) => source.SetResult(await handler(transaction, cancellationToken)), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public async Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default)
  {
    long transactionId = NextTransactionId++;
    
    await Database!.Run(Logger, transactionId, async (connection, cancellationToken) =>
    {
      using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(GetCancellationToken(), cancellationToken);
      using DbTransaction dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

      Transaction transaction = new(connection, transactionId, this, linkedCancellationTokenSource.Token);
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction begin.");

      try
      {
        await handler(transaction, linkedCancellationTokenSource.Token);
        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction commit.");

        await dbTransaction.CommitAsync(linkedCancellationTokenSource.Token);
      }
      catch (Exception exception)
      {
        Logger.Log(LogLevel.Warn, $"[Transaction #{transaction.Id}] Transaction rollback due to exception: [{exception.GetType().Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}");

        await dbTransaction.RollbackAsync(linkedCancellationTokenSource.Token);
        throw;
      }
    }, cancellationToken);
  }
}

using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Database;

using Collections;

public sealed class Database
{
  public static async Task<Database> Open(string path, CancellationToken cancellationToken)
  {
    Database database = new(path);
    await database.Connection.OpenAsync(cancellationToken);

    return database;
  }

  private Database(string path)
  {
    Connection = new()
    {
      ConnectionString = new SQLiteConnectionStringBuilder
      {
        DataSource = path
      }.ConnectionString
    };
    WaitQueue = new();
  }

  private readonly SQLiteConnection Connection;
  private readonly WaitQueue<(TaskCompletionSource source, TransactionCallback callback, CancellationToken cancellationToken)> WaitQueue;

  public delegate Task TransactionCallback(SQLiteConnection connection, CancellationToken cancellationToken);
  public delegate Task<T> TransactionCallback<T>(SQLiteConnection connection, CancellationToken cancellationToken);

  private Task? WaitQueueTask;
  private async Task RunTransactionWaitQueue()
  {
    while (WaitQueue.Count > 0)
    {
      var (source, callback, cancellationToken) = await WaitQueue.Dequeue(CancellationToken.None);
      try
      {
        SQLiteTransaction transaction = (SQLiteTransaction)await Connection.BeginTransactionAsync(cancellationToken);

        try
        {
          await callback(Connection, cancellationToken);
        }
        catch
        {
          await transaction.RollbackAsync(cancellationToken);
          throw;
        }

        await transaction.CommitAsync(cancellationToken);
        source.SetResult();
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  private async void RunTransactionQueue()
  {
    if (WaitQueueTask != null)
    {
      return;
    }

    TaskCompletionSource source = new();
    WaitQueueTask = source.Task;

    new Thread(() =>
    {
      try
      {
        RunTransactionWaitQueue().Wait();
      }
      catch (AggregateException exception)
      {
        source.SetException(exception.GetBaseException());
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
      finally
      {
        source.SetResult();
      }
    }).Start();

    try
    {
      await source.Task;
    }
    finally
    {
      WaitQueueTask = null;
    }
  }

  public async Task RunTransaction(TransactionCallback callback, CancellationToken cancellationToken)
  {
    TaskCompletionSource source = new();

    Task waitTask = WaitQueue.Enqueue((source, callback, cancellationToken), cancellationToken);
    RunTransactionQueue();
    await waitTask;
    await source.Task;
  }

  public async Task<T> RunTransaction<T>(TransactionCallback<T> callback, CancellationToken cancellationToken)
  {
    TaskCompletionSource<T> source = new();

    try { await RunTransaction(async (connection, cancellationToken) => source.SetResult(await callback(connection, cancellationToken)), cancellationToken); }
    catch (Exception exception) { source.SetException(exception); }

    return await source.Task;
  }

  public Task Close() => Connection.CloseAsync();
}

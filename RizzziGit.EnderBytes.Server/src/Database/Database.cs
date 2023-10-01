using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Database;

using Collections;
using Resources;

public sealed class Database
{
  public static async Task<Database> Open(MainResourceManager resourceManager, string path, CancellationToken cancellationToken)
  {
    Database database = new(resourceManager, path);
    await database.Connection.OpenAsync(cancellationToken);

    _ = database.RunTransactionQueue();
    return database;
  }

  private Database(MainResourceManager resourceManager, string path)
  {
    Connection = new()
    {
      ConnectionString = new SQLiteConnectionStringBuilder
      {
        DataSource = path,
        JournalMode = SQLiteJournalModeEnum.Memory
      }.ConnectionString
    };
    Logger = new("Database");

    resourceManager.Logger.Subscribe(Logger);
    WaitQueue = new();
  }

  private readonly EnderBytesLogger Logger;
  private readonly SQLiteConnection Connection;
  private readonly WaitQueue<(TaskCompletionSource source, TransactionCallback callback, CancellationToken cancellationToken)> WaitQueue;

  public delegate Task TransactionCallback(SQLiteConnection connection, CancellationToken cancellationToken);
  public delegate Task<T> TransactionCallback<T>(SQLiteConnection connection, CancellationToken cancellationToken);

  private Task? WaitQueueTask;
  private async Task RunTransactionWaitQueue()
  {
    while (true)
    {
      var (source, callback, cancellationToken) = await WaitQueue.Dequeue(CancellationToken.None);
      try
      {
        Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Begin transaction.");
        SQLiteTransaction transaction = (SQLiteTransaction)await Connection.BeginTransactionAsync(cancellationToken);

        try
        {
          await callback(Connection, cancellationToken);
        }
        catch
        {
          Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Rollback failed transaction.");
          await transaction.RollbackAsync(cancellationToken);
          throw;
        }

        Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Commit successful transaction.");
        await transaction.CommitAsync(cancellationToken);
        source.SetResult();
      }
      catch (Exception exception)
      {
        source.SetException(exception);
      }
    }
  }

  public async Task RunTransactionQueue()
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

  public async Task Close()
  {
    await Connection.CloseAsync();
    await (WaitQueueTask ?? Task.CompletedTask);
  }
}

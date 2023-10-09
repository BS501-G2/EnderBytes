using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Database;

using System.Text;
using Collections;

using WhereClause = Dictionary<string, (string condition, object? value, string? collate)>;
using ValueClause = Dictionary<string, object?>;

public sealed class Database
{
  public delegate Task TransactionCompleteHandler(SQLiteConnection connection);

  public static async Task<Database> Open(string dir, string name, CancellationToken cancellationToken)
  {
    if (!Directory.Exists(dir))
    {
      Directory.CreateDirectory(dir);
    }

    Database database = new(dir, name);
    await database.Connection.OpenAsync(cancellationToken);

    return database;
  }

  private Database(string dir, string name)
  {
    Connection = new()
    {
      ConnectionString = new SQLiteConnectionStringBuilder
      {
        DataSource = Path.Join(dir, $"{name}.sqlite3"),
        JournalMode = SQLiteJournalModeEnum.Memory
      }.ConnectionString
    };

    Name = name;
    Logger = new("Database");

    WaitQueue = new();
  }

  public readonly Logger Logger;
  private readonly SQLiteConnection Connection;
  private readonly WaitQueue<(TaskCompletionSource source, TransactionCallback callback, CancellationToken cancellationToken)> WaitQueue;

  public delegate Task TransactionCallback(SQLiteConnection connection, CancellationToken cancellationToken);
  public delegate Task<T> TransactionCallback<T>(SQLiteConnection connection, CancellationToken cancellationToken);

  private Task? WaitQueueTask;
  private SQLiteTransaction? Transaction;
  private readonly WeakKeyDictionary<SQLiteTransaction, List<TransactionCompleteHandler>> TransactionFailureHandlers = new();
  private readonly WeakKeyDictionary<SQLiteTransaction, List<TransactionCompleteHandler>> TransactionSuccessHandlers = new();

  public readonly string Name;

  public void RegisterOnTransactionCompleteHandlers(TransactionCompleteHandler? onSuccessHandler, TransactionCompleteHandler? onFailureHandler)
  {
    lock (this)
    {
      if (Transaction == null)
      {
        return;
      }

      if (TransactionSuccessHandlers.TryGetValue(Transaction, out List<TransactionCompleteHandler>? onSuccessHandlers) && onSuccessHandler != null)
      {
        onSuccessHandlers.Add(onSuccessHandler);
      }

      if (TransactionFailureHandlers.TryGetValue(Transaction, out List<TransactionCompleteHandler>? onFailureHandlers) && onFailureHandler != null)
      {
        onFailureHandlers.Add(onFailureHandler);
      }
    }
  }

  private async Task RunTransactionWaitQueue(CancellationToken waitQueueCancellationToken)
  {
    while (true)
    {
      var (source, callback, cancellationToken) = await WaitQueue.Dequeue(waitQueueCancellationToken);
      try
      {
        List<TransactionCompleteHandler> transactionFailureHandlers = [];
        List<TransactionCompleteHandler> transactionSuccessHandlers = [];

        try
        {
          Logger.Log(Logger.LOGLEVEL_VERBOSE, "Begin transaction.");
          await using SQLiteTransaction transaction = (SQLiteTransaction)await Connection.BeginTransactionAsync(cancellationToken);

          lock (this)
          {
            Transaction = transaction;
            TransactionFailureHandlers.Add(transaction, transactionFailureHandlers);
            TransactionSuccessHandlers.Add(transaction, transactionSuccessHandlers);
          }

          try
          {
            await callback(Connection, cancellationToken);
          }
          catch
          {
            Logger.Log(Logger.LOGLEVEL_WARN, "Rollback failed transaction.");
            await transaction.RollbackAsync(cancellationToken);
            throw;
          }

          Logger.Log(Logger.LOGLEVEL_VERBOSE, "Commit successful transaction.");
          await transaction.CommitAsync(cancellationToken);

          transactionSuccessHandlers.Reverse();
          foreach (TransactionCompleteHandler handler in transactionSuccessHandlers)
          {
            await handler(Connection);
          }
          source.SetResult();
        }
        catch (Exception exception)
        {
          transactionFailureHandlers.Reverse();
          foreach (TransactionCompleteHandler handler in transactionFailureHandlers)
          {
            await handler(Connection);
          }
          source.SetException(exception);
        }
      }
      finally
      {
        lock (this)
        {
          Transaction = null;
        }
      }
    }
  }

  public async Task RunTransactionQueue(CancellationToken waitQueueCancellationToken)
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
        RunTransactionWaitQueue(waitQueueCancellationToken).Wait();
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

  public async Task<ulong> Insert(SQLiteConnection connection, string table, ValueClause data, CancellationToken cancellationToken)
  {
    string commandString;
    {
      StringBuilder commandStringBuilder = new();

      commandStringBuilder.Append($"insert into {table}");
      if (data.Count != 0)
      {
        lock (data)
        {
          commandStringBuilder.Append('(');

          for (int index = 0; index < data.Count; index++)
          {
            if (index != 0)
            {
              commandStringBuilder.Append(',');
            }

            commandStringBuilder.Append(data.ElementAt(index).Key);
          }

          commandStringBuilder.Append($") values ({connection.ParamList(data.Count)});");
        }
      }

      commandString = commandStringBuilder.ToString();
      commandStringBuilder.Clear();
    }

    await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. data.Values]);
    return (ulong)connection.LastInsertRowId;
  }

  public async Task<bool> Delete(SQLiteConnection connection, string table, WhereClause where, CancellationToken cancellationToken)
  {
    List<object?> parameters = [];

    string commandString;
    {
      StringBuilder commandStringBuilder = new();

      commandStringBuilder.Append($"delete from {table}");

      if (where.Count != 0)
      {
        commandStringBuilder.Append($" where ");

        for (int index = 0; index < where.Count; index++)
        {
          if (index != 0)
          {
            commandStringBuilder.Append(" and ");
          }

          var whereEntry = where.ElementAt(index);
          commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
          parameters.Add(whereEntry.Value.value);
        }
      }

      commandString = commandStringBuilder.ToString();
      commandStringBuilder.Clear();
    }

    return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
  }

  public async Task<SQLiteDataReader> Select(
    SQLiteConnection connection,
    string table,
    WhereClause where,
    (int? offset, int length)? limit,
    (string column, string orderBy)? order,
    CancellationToken cancellationToken
  )
  {
    List<object?> parameters = [];

    string commandString;
    {
      StringBuilder commandStringBuilder = new();

      commandStringBuilder.Append($"select * from {table}");

      if (where.Count != 0)
      {
        commandStringBuilder.Append($" where ");

        for (int index = 0; index < where.Count; index++)
        {
          if (index != 0)
          {
            commandStringBuilder.Append(" and ");
          }

          var whereEntry = where.ElementAt(index);
          commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
          if (whereEntry.Value.collate != null) {
            commandStringBuilder.AppendLine($" collate {whereEntry.Value.collate}");
          }
          parameters.Add(whereEntry.Value.value);
        }
      }

      if (limit != null)
      {
        if (limit.Value.offset != null)
        {
          commandStringBuilder.Append($" limit {limit.Value.offset} {limit.Value.length}");
        }
        else
        {
          commandStringBuilder.Append($" limit {limit.Value.length}");
        }
      }

      if (order != null)
      {
        commandStringBuilder.Append($" order by {order.Value.column} {order.Value.orderBy}");
      }

      commandString = commandStringBuilder.ToString();
      commandStringBuilder.Clear();
    }

    return await connection.ExecuteReaderAsync($"{commandString};", cancellationToken, [.. parameters]);
  }

  public async Task<bool> Update(SQLiteConnection connection, string table, WhereClause where, ValueClause data, CancellationToken cancellationToken)
  {
    if (data.Count == 0)
    {
      return false;
    }

    List<object?> parameters = [];

    string commandString;
    {
      StringBuilder commandStringBuilder = new();

      commandStringBuilder.Append($"update {table}");
      if (data.Count != 0)
      {
        commandStringBuilder.Append(" set ");

        for (int index = 0; index < data.Count; index++)
        {
          if (index != 0)
          {
            commandStringBuilder.Append(", ");
          }

          KeyValuePair<string, object?> dataEntry = data.ElementAt(index);
          commandStringBuilder.Append($"{dataEntry.Key} = ({{{parameters.Count}}})");
          parameters.Add(dataEntry.Value);
        }
      }

      if (where.Count != 0)
      {
        commandStringBuilder.Append($" where ");

        for (int index = 0; index < where.Count; index++)
        {
          if (index != 0)
          {
            commandStringBuilder.Append(" and ");
          }

          var whereEntry = where.ElementAt(index);
          commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
          parameters.Add(whereEntry.Value.value);
        }
      }

      commandString = commandStringBuilder.ToString();
      commandStringBuilder.Clear();
    }

    return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
  }

  public async Task Close()
  {
    await Connection.CloseAsync();
    await (WaitQueueTask ?? Task.CompletedTask);
    WaitQueue.Dispose();
  }
}

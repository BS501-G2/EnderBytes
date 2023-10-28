using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Database;

using Collections;

public sealed class DatabaseTransaction(Database database, SqliteConnection connection)
{
  public delegate Task TransactionFailureHandler(DatabaseTransaction transaction, Exception exception);
  public delegate Task TransactionSuccessHandler(DatabaseTransaction transaction);

  public readonly Database Database = database;
  public readonly SqliteConnection Connection = connection;
  public Logger Logger => Database.Logger;

  private readonly List<TransactionFailureHandler> OnFailureHandlers = [];
  private readonly List<TransactionSuccessHandler> OnSuccessHandlers = [];

  public void OnFailure(TransactionFailureHandler handler) => OnFailureHandlers.Add(handler);
  public void OnSuccess(TransactionSuccessHandler handler) => OnSuccessHandlers.Add(handler);

  public async Task Run(Database.AsyncTransactionHandler handler, CancellationToken cancellationToken)
  {
    using SqliteTransaction transaction = Connection.BeginTransaction(IsolationLevel.Serializable, false);

    try
    {
      await handler(this, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();
      transaction.Commit();
      foreach (TransactionSuccessHandler onSuccess in OnSuccessHandlers.Reverse<TransactionSuccessHandler>())
      {
        await onSuccess(this);
      }
    }
    catch (Exception exception)
    {
      transaction.Rollback();
      foreach (TransactionFailureHandler onFailure in OnFailureHandlers.Reverse<TransactionFailureHandler>())
      {
        await onFailure(this, exception);
      }
      throw;
    }
  }

  private static void CommandText(SqliteCommand command, string sql, params object?[] sqlParams)
  {
    string[] paramStrings = new string[sqlParams.Length];
    for (int paramIndex = 0; paramIndex < paramStrings.Length; paramIndex++)
    {
      string paramName = paramStrings[paramIndex] = $"${paramIndex}";
      object? paramValue = sqlParams[paramIndex];

      int existing = command.Parameters.IndexOf(paramName);
      if (existing > -1)
      {
        command.Parameters.RemoveAt(existing);
      }

      command.Parameters.Add(new(paramName, paramValue));
    }

    command.CommandText = string.Format(sql, paramStrings);
  }

  public string ParamList(int count) => ParamList(new Range(0, count));
  public string ParamList(Range range)
  {
    DefaultInterpolatedStringHandler builder = new();
    for (int iter = range.Start.Value; iter < range.End.Value; iter++)
    {
      if (iter != range.Start.Value)
      {
        builder.AppendLiteral($",");
      }
      builder.AppendLiteral($"{{{iter}}}");
    }
    return builder.ToStringAndClear();
  }

  public int ExecuteNonQuery(string sql, params object?[] sqlParams)
  {
    SqliteCommand command = Connection.CreateCommand();
    CommandText(command, sql, sqlParams);
    Logger.Log(LogLevel.Verbose, $"Non-Query: {string.Format(sql, sqlParams)}");
    return command.ExecuteNonQuery();
  }

  public SqliteDataReader ExecuteReader(string sql, params object?[] sqlParams) => ExecuteReader(sql, CommandBehavior.Default, sqlParams);
  public SqliteDataReader ExecuteReader(string sql, CommandBehavior behavior, params object?[] sqlParams)
  {
    SqliteCommand command = Connection!.CreateCommand();
    CommandText(command, sql, sqlParams);
    Logger.Log(LogLevel.Verbose, $"Query: {string.Format(sql, sqlParams)}");
    return command.ExecuteReader(behavior);
  }

  public object? ExecuteScalar(string sql, params object?[] sqlParams)
  {
    SqliteCommand command = Connection!.CreateCommand();
    CommandText(command, sql, sqlParams);
    Logger.Log(LogLevel.Verbose, $"Scalar: {string.Format(sql, sqlParams)}");
    return command.ExecuteScalar();
  }
}

public sealed class Database : Service
{
  public delegate Task AsyncTransactionHandler(DatabaseTransaction transaction, CancellationToken cancellationToken);
  public delegate Task<T> AsyncTransactionHandler<T>(DatabaseTransaction transaction, CancellationToken cancellationToken);
  public delegate void TransactionHandler(DatabaseTransaction transaction);
  public delegate T TransactionHandler<T>(DatabaseTransaction transaction);

  public Database(Server server, string path, string name) : base("Database", server)
  {
    Server = server;
    Path = path;
    DatabaseFile = System.IO.Path.Join(Path, $"{name}.sqlite3");
  }

  public readonly Server Server;
  public readonly string Path;
  public readonly string DatabaseFile;

  private SqliteConnection? Connection;
  private readonly WaitQueue<TaskCompletionSource<(TaskCompletionSource source, CancellationToken cancellationToken)>> WaitQueue = new();

  public void ValidateTransaction(SqliteTransaction transaction)
  {
    if (transaction.Connection != Connection)
    {
      throw new ArgumentException("Invalid transaction.", nameof(transaction));
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    lock (this)
    {
      if (Connection != null)
      {
        throw new InvalidOperationException("Database is already open.");
      }

      if (!Directory.Exists(Path))
      {
        Directory.CreateDirectory(Path);
      }

      Connection = new()
      {
        ConnectionString = new SqliteConnectionStringBuilder()
        {
          DataSource = DatabaseFile
        }.ConnectionString
      };

      Connection.Open();
    }

    return Task.CompletedTask;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await Task.Run(async () =>
    {
      while (true)
      {
        var source = await WaitQueue.Dequeue(cancellationToken);
        TaskCompletionSource innerSource = new();
        source.SetResult((innerSource, cancellationToken));
        await innerSource.Task;
      }
    }, CancellationToken.None);

    while (WaitQueue.Count != 0)
    {
      (await WaitQueue.Dequeue(CancellationToken.None)).SetCanceled(CancellationToken.None);
    }
  }

  protected override Task OnStop(Exception? exception)
  {
    lock (this)
    {
      Connection!.Dispose();
      Connection = null;
    }

    return Task.CompletedTask;
  }

  public async Task RunTransaction(AsyncTransactionHandler handler, CancellationToken cancellationToken)
  {
    TaskCompletionSource<(TaskCompletionSource source, CancellationToken cancellationToken)> source = new();
    await WaitQueue.Enqueue(source, cancellationToken);
    var innerSource = await source.Task;

    CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerSource.cancellationToken);
    DatabaseTransaction transaction = new(this, Connection!);

    try
    {
      await transaction.Run(handler, cancellationTokenSource.Token);
    }
    finally
    {
      innerSource.source.SetResult();
      cancellationTokenSource.Dispose();
    }
  }

  public async Task<T> RunTransaction<T>(AsyncTransactionHandler<T> handler, CancellationToken cancellationToken)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await RunTransaction(async (transaction, cancellationToken) =>
      {
        source.SetResult(await handler(transaction, cancellationToken));
      }, cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }

  public Task RunTransaction(TransactionHandler handler, CancellationToken cancellationToken) => RunTransaction((transaction, cancellationToken) =>
  {
    handler(transaction);
    return Task.CompletedTask;
  }, cancellationToken);

  public async Task<T> RunTransaction<T>(TransactionHandler<T> handler, CancellationToken cancellationToken)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await RunTransaction((transaction, cancellationToken) =>
      {
        source.SetResult(handler(transaction));
        return Task.CompletedTask;
      }, cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }
}

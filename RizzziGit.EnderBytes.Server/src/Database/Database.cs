using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Database;

using Collections;
using RizzziGit.EnderBytes.Resources;

public sealed class DatabaseTransaction(Database database, SqliteConnection connection)
{
  public delegate void TransactionFailureHandler(DatabaseTransaction transaction, Exception exception);
  public delegate void TransactionSuccessHandler(DatabaseTransaction transaction);

  public readonly Database Database = database;
  public readonly SqliteConnection Connection = connection;
  public Logger Logger => Database.Logger;

  public event TransactionFailureHandler? Failed;
  public event TransactionSuccessHandler? Success;

  public void Run(Database.TransactionHandler handler, CancellationToken cancellationToken)
  {
    using SqliteTransaction transaction = Connection.BeginTransaction(IsolationLevel.Serializable, false);

    try
    {
      handler(this);

      cancellationToken.ThrowIfCancellationRequested();
      transaction.Commit();
      foreach (TransactionSuccessHandler onSuccess in (Success?.GetInvocationList().Reverse() ?? []).Cast<TransactionSuccessHandler>())
      {
        onSuccess(this);
      }
    }
    catch (Exception exception)
    {
      transaction.Rollback();
      foreach (TransactionFailureHandler onFailure in (Failed?.GetInvocationList().Reverse() ?? []).Cast<TransactionFailureHandler>())
      {
        onFailure(this, exception);
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
  public static string GetDatabaseFilePath(string path, string name) => System.IO.Path.Join(path, $"{name}.sqlite3");

  public delegate Task AsyncTransactionHandler(DatabaseTransaction transaction, CancellationToken cancellationToken);
  public delegate Task<T> AsyncTransactionHandler<T>(DatabaseTransaction transaction, CancellationToken cancellationToken);
  public delegate void TransactionHandler(DatabaseTransaction transaction);
  public delegate T TransactionHandler<T>(DatabaseTransaction transaction);

  public Database(IMainResourceManager main, string path, string name) : base("Database")
  {
    Server = main.Server;
    Path = path;
    DatabaseFile = GetDatabaseFilePath(Path, name);

    main.Logger.Subscribe(Logger);
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
          DataSource = DatabaseFile,
          Cache = SqliteCacheMode.Private
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

  public async Task RunTransaction(TransactionHandler handler, CancellationToken cancellationToken)
  {
    TaskCompletionSource<(TaskCompletionSource source, CancellationToken cancellationToken)> source = new();
    await WaitQueue.Enqueue(source, cancellationToken);
    var innerSource = await source.Task;

    CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerSource.cancellationToken);
    DatabaseTransaction transaction = new(this, Connection!);

    try
    {
      transaction.Run(handler, cancellationTokenSource.Token);
    }
    finally
    {
      innerSource.source.SetResult();
      cancellationTokenSource.Dispose();
    }
  }

  public async Task<T> RunTransaction<T>(TransactionHandler<T> handler, CancellationToken cancellationToken)
  {
    TaskCompletionSource<T> source = new();
    try
    {
      await RunTransaction((transaction) => source.SetResult(handler(transaction)), cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }
}

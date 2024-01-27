using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Services;

using Framework.Logging;
using Framework.Services;

using Utilities;

public sealed partial class ResourceService
{
  public abstract class ResourceManager(ResourceService service, Scope scope, string name, int version) : Service(name, service)
  {
    protected const string COLUMN_ID = "Id";
    protected const string COLUMN_CREATE_TIME = "CreateTime";
    protected const string COLUMN_UPDATE_TIME = "UpdateTime";

    public readonly ResourceService Service = service;
    public readonly Scope Scope = scope;

    public int Version = version;

    protected SQLiteConnection Database => Service.GetDatabase(Scope);

    protected abstract void Upgrade(Transaction transaction, int oldVersion = default);

    protected Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default) => Service.Transact(this, handler, cancellationToken);
    protected Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default) => Service.Transact(this, handler, cancellationToken);

    protected void ThrowIfInvalidScope(Transaction transaction)
    {
      if (transaction.Scope != Scope)
      {
        throw new ArgumentException($"Invalid scope: {Scope} expected, {transaction.Scope} proided.", nameof(transaction));
      }
    }

    private SQLiteCommand CreateCommand(Transaction transaction, string sql, object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);

      SQLiteCommand command = Database.CreateCommand();
      command.CommandText = string.Format(sql, createParameterArray());

      return command;

      string[] createParameterArray()
      {
        int index = 0;
        return parameters.Select((e) =>
        {
          try
          {
            string parameterName = $"${index}";
            command.Parameters.Add(new(parameterName, parameters[index]));

            return parameterName;
          }
          finally
          {
            index++;
          }
        }).ToArray();
      }
    }

    private void LogSql(string type, string sqlQuery, params object?[] parameters)
    {
      Logger.Log(LogLevel.Debug, $"SQL {type} on {Scope}: {string.Format(sqlQuery, parameters)}");
    }

    public delegate T SqlQueryDataHandler<T>(SQLiteDataReader reader);
    public delegate void SqlQueryDataHandler(SQLiteDataReader reader);
    public delegate IEnumerable<T> SqlQueryDataEnumerator<T>(SQLiteDataReader reader);

    protected T SqlQuery<T>(Transaction transaction, SqlQueryDataHandler<T> dataHandler, string sqlQuery, params object?[] parameters) => SqlQuery<T>(transaction, (reader) => [dataHandler(reader)], sqlQuery, parameters).First();

    protected void SqlQuery(Transaction transaction, SqlQueryDataHandler dataHandler, string sqlQuery, params object?[] parameters) => SqlQuery<byte>(transaction, (reader) =>
    {
      dataHandler(reader);
      return [];
    }, sqlQuery, parameters);

    protected IEnumerable<T> SqlQuery<T>(Transaction transaction, SqlQueryDataEnumerator<T> dataHandler, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SQLiteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql("Query", sqlQuery, parameters);
      SQLiteDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);

      foreach (T item in dataHandler(reader))
      {
        yield return item;
      }
    }

    protected int SqlNonQuery(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SQLiteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql("Non-query", sqlQuery, parameters);
      return command.ExecuteNonQuery();
    }

    protected object? SqlScalar(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SQLiteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql("Scalar", sqlQuery, parameters);
      return command.ExecuteScalar();
    }

    protected override async Task OnStart(CancellationToken cancellationToken)
    {
      await Transact((transaction, cancellationToken) =>
      {
        SqlNonQuery(transaction, $"create table if not exists __VERSIONS(Name varchar(128) primary key not null, Version integer not null);");
        int? version = null;

        {
          version = SqlQuery(transaction, (reader) => {
            if (reader.Read())
            {
              return reader.GetInt32Optional(reader.GetOrdinal("Version"));
            }

            return null;
          }, "select Version from __VERSIONS where Name = {0};", Name);
        }

        if (version != Version)
        {
          if (version == null)
          {
            SqlNonQuery(transaction, $"create table {Name}({COLUMN_ID} integer primary key autoincrement, {COLUMN_CREATE_TIME} integer not null, {COLUMN_UPDATE_TIME} integer not null);");
            Upgrade(transaction);
          }
          else
          {
            Upgrade(transaction, (int)version);
          }

          if (SqlNonQuery(transaction, $"update __VERSIONS set Version = {{0}} where Name = {{1}};", Version, Name) == 0)
          {
            SqlNonQuery(transaction, $"insert into __VERSIONS (Name, Version) values ({{0}}, {{1}});", Name, Version);
          }
        }
      }, cancellationToken);
    }
  }
}

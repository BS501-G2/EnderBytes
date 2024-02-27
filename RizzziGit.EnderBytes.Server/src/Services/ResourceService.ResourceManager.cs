using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Framework.Logging;
using Framework.Services;
using RizzziGit.EnderBytes.DatabaseWrappers;
using Utilities;

public sealed partial class ResourceService
{
  public abstract class ResourceManager(ResourceService service, string name, int version) : Service(name, service)
  {
    protected const string COLUMN_ID = "Id";
    protected const string COLUMN_CREATE_TIME = "CreateTime";
    protected const string COLUMN_UPDATE_TIME = "UpdateTime";

    public readonly ResourceService Service = service;

    public readonly int Version = version;

    protected Database DatabaseWrapper => Service.Database!;

    protected abstract void Upgrade(Transaction transaction, int oldVersion = default, CancellationToken cancellationToken = default);

    protected Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default) => Service.Transact(handler, cancellationToken);
    protected Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default) => Service.Transact(handler, cancellationToken);

    private DbCommand CreateCommand(Transaction transaction, string sql, object?[] parameters)
    {
      DbCommand command = transaction.Connection.CreateCommand();
      command.CommandText = string.Format(sql, createParameterArray());

      return command;

      string[] createParameterArray()
      {
        int index = 0;
        return parameters.Select((e) =>
        {
          try
          {
            string parameterName = $"{index}";

            command.Parameters.Add(DatabaseWrapper.CreateParameter(parameterName, parameters[index]));
            return DatabaseWrapper.ToParameterName(parameterName);
          }
          finally
          {
            index++;
          }
        }).ToArray();
      }
    }

    private void LogSql(Transaction transaction, string type, string sqlQuery, params object?[] parameters)
    {
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] SQL {type}: {string.Format(sqlQuery, parameters)}");
    }

    public delegate T SqlQueryDataHandler<T>(DbDataReader reader);
    public delegate void SqlQueryDataHandler(DbDataReader reader);
    public delegate IEnumerable<T> SqlQueryDataEnumeratorHandler<T>(DbDataReader reader);

    protected T SqlQuery<T>(Transaction transaction, SqlQueryDataHandler<T> dataHandler, string sqlQuery, params object?[] parameters) => SqlEnumeratedQuery<T>(transaction, (reader) => [dataHandler(reader)], sqlQuery, parameters).First();

    protected void SqlQuery(Transaction transaction, SqlQueryDataHandler dataHandler, string sqlQuery, params object?[] parameters) => _ = SqlEnumeratedQuery<byte>(transaction, (reader) =>
    {
      dataHandler(reader);

      return [0];
    }, sqlQuery, parameters).First();

    protected IEnumerable<T> SqlEnumeratedQuery<T>(Transaction transaction, SqlQueryDataEnumeratorHandler<T> dataHandler, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Query", sqlQuery, parameters);
      using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);

      foreach (T item in dataHandler(reader))
      {
        yield return item;
      }
    }

    protected int SqlNonQuery(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Non-query", sqlQuery, parameters);
      return command.ExecuteNonQuery();
    }

    protected object? SqlScalar(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Scalar", sqlQuery, parameters);
      return command.ExecuteScalar();
    }

    protected sealed override Task OnStop(Exception? exception) => base.OnStop(exception);
    protected sealed override Task OnRun(CancellationToken cancellationToken) => base.OnRun(cancellationToken);
    protected sealed override async Task OnStart(CancellationToken cancellationToken)
    {
      await Transact((transaction, cancellationToken) =>
      {
        SqlNonQuery(transaction, $"create table if not exists __VERSIONS(Name varchar(128) primary key not null, Version bigint not null);");
        int? version = null;

        {
          version = SqlQuery(transaction, (reader) =>
          {
            if (reader.Read())
            {
              return reader.GetInt32Optional(reader.GetOrdinal("Version"));
            }

            return null;
          }, "select Version from __VERSIONS where Name = {0} limit 1;", Name);
        }

        if (version != Version)
        {
          if (version == null)
          {
            SqlNonQuery(transaction, $"create table {Name}({COLUMN_ID} bigint primary key auto_increment, {COLUMN_CREATE_TIME} bigint not null, {COLUMN_UPDATE_TIME} bigint not null);");
            Upgrade(transaction, cancellationToken: cancellationToken);
          }
          else
          {
            Upgrade(transaction, (int)version, cancellationToken);
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

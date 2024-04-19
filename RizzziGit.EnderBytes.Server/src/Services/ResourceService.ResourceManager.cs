using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Commons.Logging;
using Commons.Services;

using Utilities;
using DatabaseWrappers;

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

    protected abstract Task Upgrade(Transaction transaction, int oldVersion = default, CancellationToken cancellationToken = default);

    protected Task<T> Transact<T>(TransactionHandler<T> handler, CancellationToken cancellationToken = default) => Service.Transact(handler, cancellationToken);
    protected Task Transact(TransactionHandler handler, CancellationToken cancellationToken = default) => Service.Transact(handler, cancellationToken);
    protected IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler, CancellationToken cancellationToken = default) => Service.EnumeratedTransact(handler, cancellationToken);

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

    public delegate Task<T> SqlQueryTransformer<T>(DbDataReader reader);

    protected async IAsyncEnumerable<T> SqlQuery<T>(Transaction transaction, SqlQueryTransformer<T> dataHandler, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Query", sqlQuery, parameters);
      using DbDataReader reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SingleResult);

      while (reader.Read())
      {
        yield return await dataHandler(reader);
      }
    }

    protected async Task<int> SqlNonQuery(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Non-query", sqlQuery, parameters);
      return await command.ExecuteNonQueryAsync();
    }

    protected async Task<object?> SqlScalar(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      using DbCommand command = CreateCommand(transaction, sqlQuery, parameters);

      LogSql(transaction, "Scalar", sqlQuery, parameters);
      return await command.ExecuteScalarAsync();
    }

    protected sealed override Task OnStop(System.Exception? exception) => base.OnStop(exception);
    protected sealed override Task OnRun(CancellationToken cancellationToken) => base.OnRun(cancellationToken);
    protected sealed override async Task OnStart(CancellationToken cancellationToken)
    {
      await Transact(async (transaction, cancellationToken) =>
      {
        await SqlNonQuery(transaction, $"create table if not exists __VERSIONS(Name varchar(128) primary key not null, Version bigint not null);");
        int? version = null;

        {
          version = await SqlQuery(transaction, (reader) => Task.FromResult(reader.GetInt32Optional(reader.GetOrdinal("Version"))), "select Version from __VERSIONS where Name = {0} limit 1;", Name).FirstAsync(cancellationToken);
        }

        if (version != Version)
        {
          if (version == null)
          {
            await SqlNonQuery(transaction, $"create table {Name}({COLUMN_ID} bigint primary key auto_increment, {COLUMN_CREATE_TIME} bigint not null, {COLUMN_UPDATE_TIME} bigint not null);");
            await Upgrade(transaction, cancellationToken: cancellationToken);
          }
          else
          {
            await Upgrade(transaction, (int)version, cancellationToken);
          }

          if (await SqlNonQuery(transaction, $"update __VERSIONS set Version = {{0}} where Name = {{1}};", Version, Name) == 0)
          {
            await SqlNonQuery(transaction, $"insert into __VERSIONS (Name, Version) values ({{0}}, {{1}});", Name, Version);
          }
        }
      }, cancellationToken);
    }
  }
}

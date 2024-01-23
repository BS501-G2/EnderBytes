using Microsoft.Data.Sqlite;

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

    protected SqliteConnection Database => Service.GetDatabase(Scope);

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

    private SqliteCommand CreateCommand(Transaction transaction, string sql, object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);

      SqliteCommand command = Database.CreateCommand();
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

    protected SqliteDataReader SqlQuery(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SqliteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      // Logger.Log(LogLevel.Verbose, $"SQL Query ({parameters.Length}): {sqlQuery}");
      Logger.Log(LogLevel.Debug, $"SQL Query on {Scope}: {string.Format(sqlQuery, parameters)}");
      return command.ExecuteReader();
    }

    protected int SqlNonQuery(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SqliteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      // Logger.Log(LogLevel.Verbose, $"SQL Non-query ({parameters.Length}): {sqlQuery}");
      Logger.Log(LogLevel.Debug, $"SQL Non-query on {Scope}: {string.Format(sqlQuery, parameters)}");
      return command.ExecuteNonQuery();
    }

    protected object? SqlScalar(Transaction transaction, string sqlQuery, params object?[] parameters)
    {
      ThrowIfInvalidScope(transaction);
      SqliteCommand command = CreateCommand(transaction, sqlQuery, parameters);

      // Logger.Log(LogLevel.Verbose, $"SQL Scalar ({parameters.Length}): {sqlQuery}");
      Logger.Log(LogLevel.Debug, $"SQL Scalar on {Scope}: {string.Format(sqlQuery, parameters)}");
      return command.ExecuteScalar();
    }

    protected override async Task OnStart(CancellationToken cancellationToken)
    {
      await Transact((transaction) =>
      {
        SqlNonQuery(transaction, $"create table if not exists __VERSIONS(Name varchar(128) primary key not null, Version integer not null);");
        int? version = null;

        {
          using SqliteDataReader reader = SqlQuery(transaction, "select Version from __VERSIONS where Name = {0};", Name);
          if (reader.Read())
          {
            version = reader.GetInt32Optional(reader.GetOrdinal("Version"));
          }
          reader.Close();
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

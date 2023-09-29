using System.Data.SQLite;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Newtonsoft.Json.Linq;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    public sealed partial class DatabaseWrapper(M manager)
    {
      public readonly M Manager = manager;
      public Database Database => Manager.Database;

      public Task<ulong> Insert(Dictionary<string, object> data, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => Insert(connection, data, cancellationToken), cancellationToken);
      public async Task<ulong> Insert(SQLiteConnection connection, Dictionary<string, object> data, CancellationToken cancellationToken)
      {
        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"insert into {Manager.Name}");
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

      public Task<bool> Delete(Dictionary<string, (string condition, object value)> where, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => Delete(connection, where, cancellationToken), cancellationToken);
      public async Task<bool> Delete(SQLiteConnection connection, Dictionary<string, (string condition, object value)> where, CancellationToken cancellationToken)
      {
        List<object> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"delete from {Manager.Name}");

          if (where.Count != 0)
          {
            commandStringBuilder.Append($" where ");

            for (int index = 0; index < where.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(" and ");
              }

              KeyValuePair<string, (string condition, object value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
      }

      public Task<IAsyncEnumerable<R>> Select(Dictionary<string, (string condition, object value)> where, int? offset, int? length, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => Select(connection, where, offset, length, cancellationToken), cancellationToken);
      public async Task<IAsyncEnumerable<R>> Select(SQLiteConnection connection, Dictionary<string, (string condition, object value)> where, int? offset, int? length, CancellationToken cancellationToken)
      {
        List<object> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"select * from {Manager.Name}");

          if (where.Count != 0)
          {
            commandStringBuilder.Append($" where ");

            for (int index = 0; index < where.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(" and ");
              }

              KeyValuePair<string, (string condition, object value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          if (length != null)
          {
            commandStringBuilder.Append($" limit {offset}{(offset != null ? $" {length}" : "")};");
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return new ResourceStream(Manager, await connection.ExecuteReaderAsync(commandString, cancellationToken, [.. parameters]));
      }

      public Task<bool> Update(Dictionary<string, (string condition, object value)> where, Dictionary<string, object> data, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => Update(connection, where, data, cancellationToken), cancellationToken);
      public async Task<bool> Update(SQLiteConnection connection, Dictionary<string, (string condition, object value)> where, Dictionary<string, object> data, CancellationToken cancellationToken)
      {
        if (data.Count == 0)
        {
          return false;
        }

        List<object> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"update {Manager.Name}");
          if (data.Count != 0)
          {
            commandStringBuilder.Append(" set ");

            for (int index = 0; index < data.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(", ");
              }

              KeyValuePair<string, object> dataEntry = data.ElementAt(index);
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

              KeyValuePair<string, (string condition, object value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
      }
    }
  }
}

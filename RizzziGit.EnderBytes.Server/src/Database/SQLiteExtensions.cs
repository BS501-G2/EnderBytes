using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Database;

public static class SQLiteExtensions
{
  public static void AddRange(this SQLiteParameterCollection parameters, params SQLiteParameter[] array) => parameters.AddRange(array);
  public static void CommandText(this SQLiteCommand command, string sql, params object?[] sqlParams)
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

      command.Parameters.Add(paramValue != null ? new(paramName, paramValue) : new(paramName));
    }

    // Console.WriteLine($"{sql} {paramStrings.Length}");
    command.CommandText = string.Format(sql, paramStrings);
    Console.WriteLine(command.CommandText);
  }

  public static string ParamList(this SQLiteConnection connection, int count) => connection.ParamList(new Range(0, count));
  public static string ParamList(this SQLiteConnection _, Range range)
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

  public static Task<int> ExecuteNonQueryAsync(this SQLiteConnection connection, string sql, CancellationToken cancellationToken, params object?[] sqlParams)
  {
    using SQLiteCommand command = connection.CreateCommand();
    command.CommandText(sql, sqlParams);
    return command.ExecuteNonQueryAsync(cancellationToken);
  }

  public static Task<SQLiteDataReader> ExecuteReaderAsync(this SQLiteConnection connection, string sql, CancellationToken cancellationToken, params object?[] sqlParams) => connection.ExecuteReaderAsync(sql, CommandBehavior.Default, cancellationToken, sqlParams);
  public static async Task<SQLiteDataReader> ExecuteReaderAsync(this SQLiteConnection connection, string sql, CommandBehavior behavior, CancellationToken cancellationToken, params object?[] sqlParams)
  {
    using SQLiteCommand command = connection.CreateCommand();
    command.CommandText(sql, sqlParams);
    return (SQLiteDataReader)await command.ExecuteReaderAsync(behavior, cancellationToken);
  }

  public static Task<object?> ExecuteScalarAsync(this SQLiteConnection connection, string sql, CancellationToken cancellationToken, params object?[] sqlParams)
  {
    using SQLiteCommand command = connection.CreateCommand();
    command.CommandText(sql, sqlParams);
    return command.ExecuteScalarAsync(cancellationToken);
  }
}

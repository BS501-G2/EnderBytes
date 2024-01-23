using Microsoft.Data.Sqlite;
using System.Text;
using System.Security.Cryptography;
using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Framework.Memory;

using Services;
using Utilities;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public abstract partial class ResourceManager(ResourceService service, ResourceService.Scope scope, string name, int version) : ResourceService.ResourceManager(service, scope, name, version)
  {
    protected sealed class ValueClause() : Dictionary<string, object?>
    {
      public ValueClause(params (string Column, object? Value)[] values) : this()
      {
        foreach (var (column, value) in values)
        {
          Add(column, value);
        }
      }

      public string Apply(List<object?> parameterList) => Apply([.. Keys], parameterList);
      public string Apply(string[] columns, List<object?> parameterList)
      {
        StringBuilder stringBuilder = new("(");

        for (int index = 0; index < columns.Length; index++)
        {
          string column = columns[index];

          if (index != 0)
          {
            stringBuilder.Append(", ");
          }

          if (TryGetValue(column, out object? value) && value != null)
          {
            stringBuilder.Append($"{{{parameterList.Count}}}");
            parameterList.Add(value);
          }
          else
          {
            stringBuilder.Append("null");
          }
        }

        stringBuilder.Append(')');
        return stringBuilder.ToString();
      }
    }

    protected abstract record WhereClause
    {
      private WhereClause() { }

      public sealed record CompareColumn(string Column, string Comparer, object? Value) : WhereClause
      {
        public override string Apply(List<object?> parameterList)
        {
          try
          {
            return $"({Column} {Comparer} {{{parameterList.Count}}})";
          }
          finally
          {
            parameterList.Add(Value);
          }
        }
      }

      public sealed record Nested(string Connector, params WhereClause[] Expressions) : WhereClause
      {
        public override string Apply(List<object?> parameterList)
        {
          StringBuilder builder = new("(");

          for (int index = 0; index < Expressions.Length; index++)
          {
            if (index != 0)
            {
              builder.Append($" {Connector} ");
            }

            WhereClause clause = Expressions[index];
            builder.Append(clause.Apply(parameterList));
          }

          builder.Append(')');
          return builder.ToString();
        }
      }

      public abstract string Apply(List<object?> parameterList);
    }

    protected sealed class SetClause() : Dictionary<string, object?>
    {
      public SetClause(params (string Column, object? Value)[] values) : this()
      {
        foreach (var (column, value) in values)
        {
          Add(column, value);
        }
      }

      public string Apply(List<object?> parameterList)
      {
        StringBuilder builder = new();

        int index = 0;
        foreach (var (column, value) in this)
        {
          if (index != 0)
          {
            builder.Append(", ");
          }

          builder.Append($"{column} = {{{parameterList.Count}}}");
          parameterList.Add(value);

          index++;
        }

        return builder.ToString();
      }
    }

    public delegate void ResourceDeleteHandler(R resource);
    public delegate void ResourceUpdateHandler(R resource, D oldData);
    public delegate void ResourceInsertHandler(R resource);

    private readonly WeakDictionary<long, R> Resources = [];

    protected abstract R NewResource(D data);
    private D CastToData(SqliteDataReader reader) => CastToData(reader,
      reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
    );
    protected abstract D CastToData(SqliteDataReader reader, long id, long createTime, long updateTime);
    protected R GetResource(D data)
    {
      if (!Resources.TryGetValue(data.Id, out R? resource))
      {
        Resources.Add(data.Id, resource = NewResource(data));
      }
      else
      {
        resource.Data = data;
      }

      return resource;
    }

    protected R Insert(ResourceService.Transaction transaction, ValueClause values)
    {
      ThrowIfInvalidScope(transaction);

      long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      values.Add(COLUMN_CREATE_TIME, insertTimestamp);
      values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

      List<object?> parameterList = [];
      string insertClause = values.Apply(parameterList);

      {
        using SqliteDataReader selectReader = SqlQuery(transaction, $"insert into {Name} ({string.Join(", ", values.Keys)}) values {insertClause}; select * from {Name} where {COLUMN_ID} = last_insert_rowid() limit 1;", [.. parameterList]);
        // using SqliteDataReader selectReader = SqlQuery(transaction, $"select * from {Name} where {COLUMN_ID} = {{0}} limit 1", newId);

        if (selectReader.Read())
        {
          R resource = GetResource(CastToData(selectReader));

          transaction.RegisterOnFailureHandler(() => Resources.Remove(resource.Id));
          return resource;
        }
      }

      throw new InvalidOperationException("Failed to get the new inserted row.");
    }

    protected IEnumerable<R> Select(ResourceService.Transaction transaction, WhereClause where)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];
      using SqliteDataReader reader = SqlQuery(transaction, $"select * from {Name} where {where.Apply(parameterList)};", [.. parameterList]);

      while (true)
      {
        yield return GetResource(CastToData(reader));
      }
    }

    protected bool Update(ResourceService.Transaction transaction, R resource, SetClause set)
    {
      ThrowIfInvalidScope(transaction);

      return Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set) != 0;
    }

    protected long Update(ResourceService.Transaction transaction, WhereClause where, SetClause set)
    {
      List<object?> parameterList = [];
      set.Add(COLUMN_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

      string whereClause = where.Apply(parameterList);
      string setClause = set.Apply(parameterList);

      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      using SqliteDataReader reader = SqlQuery(
        transaction,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select {Name}.* from {temporaryTableName} left join (select * from {Name} group by {COLUMN_ID}) {Name} on {temporaryTableName}.{COLUMN_ID} = {Name}.{COLUMN_ID}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      );

      long count = 0;
      while (reader.Read())
      {
        long affectedId = reader.GetInt64(reader.GetOrdinal(COLUMN_ID));

        if (Resources.TryGetValue(affectedId, out R? resource))
        {
          resource.Data = CastToData(reader);
        }

        count++;
      }

      return count;
    }
  }

  public abstract partial record ResourceData(long Id, long CreateTime, long UpdateTime);

  public readonly M Manager = manager;
  protected D Data { get; private set; } = data;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;
}

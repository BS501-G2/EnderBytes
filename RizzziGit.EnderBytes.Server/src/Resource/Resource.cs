using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using System.Text;
using Database;

using WhereClause = Dictionary<string, (string condition, object? value, string? collate)>;
using ValueClause = Dictionary<string, object?>;

public abstract class Resource<M, D, R> : Shared.Resources.Resource<M, D, R>
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public new abstract class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager
  {
    private const string KEY_ID = "ID";
    private const string KEY_CREATE_TIME = "CreateTime";
    private const string KEY_UPDATE_TIME = "UpdateTime";

    protected ResourceManager(MainResourceManager main, Database database, string name, int version) : base(main)
    {
      Logger = new(name);
      Main = main;
      Database = database;
      Name = name;
      Version = version;

      Main.Logger.Subscribe(Logger);
    }

    public readonly Logger Logger;
    public new readonly MainResourceManager Main;
    protected readonly Database Database;
    public readonly string Name;
    public readonly int Version;

    protected D CreateData(SqliteDataReader reader) => CreateData(reader,
      (long)reader[KEY_ID],
      (long)reader[KEY_CREATE_TIME],
      (long)reader[KEY_UPDATE_TIME]
    );

    protected abstract D CreateData(SqliteDataReader reader, long id, long createTime, long updateTime);
    protected override abstract R CreateResource(D data);

    protected abstract void OnInit(int oldVersion, DatabaseTransaction transaction);
    protected abstract void OnInit(DatabaseTransaction transaction);
    public bool Init(DatabaseTransaction transaction)
    {
      int? oldVersion = Main.TableVersion.GetVersion(transaction, Name);
      if (oldVersion == null)
      {
        transaction.ExecuteNonQuery($"create table {Name}({KEY_ID} integer primary key autoincrement);");

        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_CREATE_TIME} integer not null");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_UPDATE_TIME} integer not null");

        OnInit(transaction);
      }
      else if (oldVersion != Version)
      {
        OnInit((int)oldVersion, transaction);
        Main.TableVersion.SetVersion(transaction, Name, Version);
      }
      return oldVersion != Version;
    }

    public Task<bool> Init(CancellationToken cancellationToken) => Database.RunTransaction(Init, cancellationToken);

    protected long DbInsert(DatabaseTransaction transaction, ValueClause row)
    {
      StringBuilder sql = new();
      List<object?> sqlParams = [];

      sql.Append($"insert into {Name}");
      if (row.Count != 0)
      {
        StringBuilder columnClause = new();
        StringBuilder valuesClause = new();

        bool firstEntry = true;
        foreach (var (key, value) in row.Concat([
          new(KEY_CREATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
          new(KEY_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        ]))
        {
          if (!firstEntry)
          {
            columnClause.Append(',');
            valuesClause.Append(',');
          }
          else
          {
            firstEntry = false;
          }

          columnClause.Append(key);
          valuesClause.Append($"{{{sqlParams.Count}}}");
          sqlParams.Add(value);
        }

        sql.Append($"({columnClause}) values ({valuesClause})");
      }
      sql.Append(';');

      _ = transaction.ExecuteNonQuery(sql.ToString(), [.. sqlParams]);
      return (long)(transaction.ExecuteScalar($"select seq from sqlite_sequence where name = {{0}} limit 1;", Name) ?? throw new InvalidOperationException("Failed to get new row ID."));
    }

    protected long DbUpdate(DatabaseTransaction transaction, ValueClause set, WhereClause where)
    {
      if (set.Count == 0)
      {
        return 0;
      }

      StringBuilder sql = new();
      List<object?> sqlParams = [];

      sql.Append($"update {Name} set ");

      {
        bool firstEntry = true;
        foreach (var (key, value) in set.Concat([
          new(KEY_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        ]))
        {
          if (!firstEntry)
          {
            sql.Append(',');
          }
          else
          {
            firstEntry = false;
          }

          sql.Append($"{key}={{{sqlParams.Count}}}");
          sqlParams.Add(value);
        }
      }

      if (where.Count != 0)
      {
        sql.Append($" where ");

        bool firstEntry = true;
        foreach (var (key, (condition, value, collate)) in where)
        {
          if (!firstEntry)
          {
            sql.Append(" and ");
          }
          else
          {
            firstEntry = false;
          }

          sql.Append($"{key} {condition} {{{sqlParams.Count}}}");
          sqlParams.Add(value);

          if (collate != null)
          {
            sql.Append($" collate {collate}");
          }
        }
      }

      sql.Append(';');
      return transaction.ExecuteNonQuery(sql.ToString(), [.. sqlParams]);
    }

    protected long DbDelete(DatabaseTransaction transaction, WhereClause where)
    {
      StringBuilder sql = new();
      List<object?> sqlParams = [];

      sql.Append($"delete from {Name}");
      if (where.Count != 0)
      {
        sql.Append(" where ");

        bool firstEntry = true;
        foreach (var (key, (condition, value, collate)) in where)
        {
          if (!firstEntry)
          {
            sql.Append(" and ");
          }
          else
          {
            firstEntry = false;
          }

          sql.Append($"{key} {condition} {{{sqlParams.Count}}}");
          sqlParams.Add(value);

          if (collate != null)
          {
            sql.Append($" collate {collate}");
          }
        }
      }
      sql.Append(';');

      return transaction.ExecuteNonQuery(sql.ToString(), [.. where]);
    }

    protected SqliteDataReader DbSelect(DatabaseTransaction transaction, WhereClause where, List<string> project)
    {
      StringBuilder sql = new();
      List<object?> sqlParams = [];

      sql.Append($"select ");

      if (project.Count != 0)
      {
        bool firstEntry = true;
        foreach (string projectEntry in project)
        {
          if (!firstEntry)
          {
            sql.Append(',');
          }
          else
          {
            firstEntry = false;
          }
        }
      }
      else
      {
        sql.Append('*');
      }

      if (where.Count != 0)
      {
        sql.Append(" from ");

        bool firstEntry = true;
        foreach (var (key, (condition, value, collate)) in where)
        {
          if (!firstEntry)
          {
            sql.Append(" and ");
          }
          else
          {
            firstEntry = false;
          }

          sql.Append($"{key} {condition} {{{sqlParams.Count}}}");
          sqlParams.Add(value);

          if (collate != null)
          {
            sql.Append($" collate {collate}");
          }
        }
      }

      sql.Append(';');

      return transaction.ExecuteReader(sql.ToString(), sqlParams);
    }
  }

  public new abstract record ResourceData(long Id, long CreateTime, long UpdateTime) : Shared.Resources.Resource<M, D, R>.ResourceData(Id)
  {
    public const string KEY_CREATE_TIME = "createTime";
    public const string KEY_UPDATE_TIME = "updateTime";

    [JsonPropertyName(KEY_CREATE_TIME)]
    public long CreateTime = CreateTime;

    [JsonPropertyName(KEY_UPDATE_TIME)]
    public long UpdateTime = UpdateTime;
  }

  protected Resource(M manager, D data) : base(manager, data)
  {
  }

  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;
}

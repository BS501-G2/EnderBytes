using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Collections;

using WhereClause = Dictionary<string, (string condition, object? value, string? collate)>;
using ValueClause = Dictionary<string, object?>;
using LimitClause = (int count, int? offset);
using OrderClause = (string column, string orderBy);

public abstract class Resource<M, D, R>
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public abstract class ResourceManager
  {
    protected const string KEY_ID = "ID";
    protected const string KEY_CREATE_TIME = "CreateTime";
    protected const string KEY_UPDATE_TIME = "UpdateTime";

    public delegate void ResourceInsertHandler(DatabaseTransaction transaction, R resource);
    public delegate Task AsyncResourceInsertHandler(DatabaseTransaction transaction, R resource, CancellationToken cancellationToken);

    public delegate void ResourceDeleteHandler(DatabaseTransaction transaction, R resource);
    public delegate Task AsyncResourceDeleteHandler(DatabaseTransaction transaction, R resource, CancellationToken cancellationToken);

    public delegate void ResourceUpdateHandler(DatabaseTransaction transaction, R resource, D oldData);
    public delegate Task AsyncResourceUpdateHandler(DatabaseTransaction transaction, R resource, D oldData, CancellationToken cancellationToken);

    public sealed class ResourceMemory(ResourceManager manager) : WeakDictionary<long, R>
    {
      public readonly ResourceManager Manager = manager;

      public R ResolveFromData(D data)
      {
        if (TryGetValue(data.Id, out var value))
        {
          value.Data = data;
          return value;
        }

        R resource = Manager.CreateResource(data);
        TryAdd(data.Id, resource);
        return resource;
      }
    }

    protected ResourceManager(MainResourceManager main, Database database, string name, int version)
    {
      Logger = new(name);
      Main = main;
      Database = database;
      Name = name;
      Version = version;

      Memory = new(this);
      ResourceUpdateHandlers = [];
      ResourceDeleteHandlers = [];

      Main.Logger.Subscribe(Logger);
    }

    public readonly Logger Logger;
    public readonly MainResourceManager Main;
    protected readonly Database Database;
    public readonly string Name;
    public readonly int Version;

    protected readonly ResourceMemory Memory;
    private readonly List<AsyncResourceUpdateHandler> ResourceUpdateHandlers;
    private readonly List<AsyncResourceDeleteHandler> ResourceDeleteHandlers;

    public bool IsValid(R resource) => Memory.TryGetValue(resource.Id, out var value) && resource == value;

    public void OnResourceUpdate(AsyncResourceUpdateHandler handler) => ResourceUpdateHandlers.Add(handler);
    public void OnResourceUpdate(ResourceUpdateHandler handler) => OnResourceUpdate((transaction, id, oldData, _) =>
    {
      handler(transaction, id, oldData);
      return Task.CompletedTask;
    });

    public void OnResourceDelete(AsyncResourceDeleteHandler handler) => ResourceDeleteHandlers.Add(handler);
    public void OnResourceDelete(ResourceDeleteHandler handler) => OnResourceDelete((transaction, id, _) =>
    {
      handler(transaction, id);
      return Task.CompletedTask;
    });

    protected D CreateData(SqliteDataReader reader) => CreateData(reader,
      (long)reader[KEY_ID],
      (long)reader[KEY_CREATE_TIME],
      (long)reader[KEY_UPDATE_TIME]
    );

    protected abstract D CreateData(SqliteDataReader reader, long id, long createTime, long updateTime);

    protected abstract R CreateResource(D data);

    protected abstract void OnInit(int oldVersion, DatabaseTransaction transaction);
    protected abstract void OnInit(DatabaseTransaction transaction);

    public bool Init(DatabaseTransaction transaction)
    {
      Validate(transaction);

      int? oldVersion = Main.TableVersion.GetVersion(transaction, Name);
      if (oldVersion == null)
      {
        transaction.ExecuteNonQuery($"create table {Name}({KEY_ID} integer primary key autoincrement);");

        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_CREATE_TIME} integer not null");
        transaction.ExecuteNonQuery($"alter table {Name} add column {KEY_UPDATE_TIME} integer not null");

        OnInit(transaction);
        Main.TableVersion.SetVersion(transaction, Name, Version);
      }
      else if (oldVersion != Version)
      {
        OnInit((int)oldVersion, transaction);
        Main.TableVersion.SetVersion(transaction, Name, Version);
      }
      return oldVersion != Version;
    }

    public void Validate(DatabaseTransaction transaction)
    {
      if (transaction.Database != Database)
      {
        throw new ArgumentException("Invalid transaction.", nameof(transaction));
      }
    }

    protected R DbInsert(DatabaseTransaction transaction, ValueClause row)
    {
      Validate(transaction);

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

        sql.Append($"({columnClause})values({valuesClause})");
      }
      sql.Append(';');

      transaction.ExecuteNonQuery(sql.ToString(), [.. sqlParams]);
      using SqliteDataReader reader = DbSelect(transaction, new()
      {
        {
          KEY_ID,
          (
            "=",
            (long)(transaction.ExecuteScalar($"select seq from sqlite_sequence where name = {{0}} limit 1;", Name) ?? throw new InvalidOperationException("Failed to get new row ID.")),
            null
          )
        }
      }, []);

      if (!reader.Read())
      {
        throw new InvalidOperationException("Failed to read new resource data.");
      }

      return Memory.ResolveFromData(CreateData(reader));
    }

    protected async Task<long> DbUpdate(DatabaseTransaction transaction, ValueClause set, WhereClause where, CancellationToken cancellationToken)
    {
      Validate(transaction);

      if (set.Count == 0)
      {
        return 0;
      }

      List<object?> sqlParams = [];
      StringBuilder setCommand = new();
      {
        bool firstEntry = true;
        foreach (var (key, value) in set.Concat([
          new(KEY_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        ]))
        {
          if (!firstEntry)
          {
            setCommand.Append(',');
          }
          else
          {
            firstEntry = false;
          }

          setCommand.Append($"{key} = {{{sqlParams.Count}}}");
          sqlParams.Add(value);
        }
      }

      string setSql = setCommand.ToString();
      using SqliteDataReader reader = DbSelect(transaction, where, []);
      long count = 0;
      while (reader.Read())
      {
        R resource = Memory.ResolveFromData(CreateData(reader));
        D oldData = resource.Data;
        transaction.ExecuteNonQuery($"update set {setSql} {Name} where {KEY_ID} = {{{sqlParams.Count}}};", [.. sqlParams, resource.Id]);
        transaction.OnFailure((_, _) =>
        {
          resource.Data = oldData;
          return Task.CompletedTask;
        });

        using SqliteDataReader newReader = transaction.ExecuteReader($"select * from {Name} where {KEY_ID} = {{0}} limit 1;", [resource.Id]);
        if (!newReader.Read())
        {
          throw new InvalidOperationException("Failed to read new resource data.");
        }
        resource = Memory.ResolveFromData(CreateData(reader));

        foreach (AsyncResourceUpdateHandler handler in ResourceUpdateHandlers)
        {
          await handler(transaction, resource, oldData, cancellationToken);
        }
        count++;
      }

      return count;
    }

    protected async Task<long> DbDelete(DatabaseTransaction transaction, WhereClause where, CancellationToken cancellationToken)
    {
      Validate(transaction);

      using SqliteDataReader reader = DbSelect(transaction, where, []);

      long count = 0;
      while (reader.Read())
      {
        R resource = Memory.ResolveFromData(CreateData(reader));

        transaction.ExecuteNonQuery($"delete from {Name} where {KEY_ID} = {{0}}", resource.Id);
        Memory.Remove(resource.Id);
        transaction.OnFailure((_, _) =>
        {
          Memory.Add(resource.Id, resource);
          return Task.CompletedTask;
        });

        foreach (AsyncResourceDeleteHandler handler in ResourceDeleteHandlers)
        {
          await handler(transaction, resource, cancellationToken);
        }

        count++;
      }

      return count;
    }

    protected IEnumerable<R> DbStream(DatabaseTransaction transaction, WhereClause where, LimitClause? limit = null, List<OrderClause>? order = null) => Stream(DbSelect(transaction, where, [], limit, order));
    protected SqliteDataReader DbSelect(DatabaseTransaction transaction, WhereClause where, List<string> project, LimitClause? limit = null, List<OrderClause>? order = null)
    {
      Validate(transaction);

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

      sql.Append($" from {Name}");
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

      if (limit != null)
      {
        sql.Append($" limit {limit.Value.count}");

        if (limit.Value.offset != null)
        {
          sql.Append($" offset {limit.Value.offset}");
        }
      }

      if (order != null)
      {
        sql.Append(" order by ");

        bool firstEntry = true;
        foreach (var (column, orderBy) in order)
        {
          if (!firstEntry)
          {
            sql.Append(',');
          }
          else
          {
            firstEntry = false;
          }

          sql.Append($"{column} {orderBy}");
        }
      }

      sql.Append(';');
      return transaction.ExecuteReader(sql.ToString(), [.. sqlParams]);
    }

    public IEnumerable<R> Stream(SqliteDataReader reader)
    {
      while (reader.Read())
      {
        yield return Memory.ResolveFromData(CreateData(reader));
      }
    }

    public R? GetById(DatabaseTransaction database, long id)
    {
      using var reader = DbSelect(database, new()
      {
        { KEY_ID, ("=", id, null) }
      }, [], (1, null), null);

      while (reader.Read())
      {
        return Memory.ResolveFromData(CreateData(reader));
      }

      return null;
    }

    public Task Delete(DatabaseTransaction transaction, R resource, CancellationToken cancellationToken) => DbDelete(transaction, new()
    {
      { KEY_ID, ("=", resource.Id, null) }
    }, cancellationToken);
  }

  public abstract record ResourceData(long Id, long CreateTime, long UpdateTime)
  {
    public const string KEY_ID = "id";
    [JsonPropertyName(KEY_ID)]
    public long Id = Id;

    public const string KEY_CREATE_TIME = "createTime";
    [JsonPropertyName(KEY_CREATE_TIME)]
    public long CreateTime = CreateTime;

    public const string KEY_UPDATE_TIME = "updateTime";
    [JsonPropertyName(KEY_UPDATE_TIME)]
    public long UpdateTime = UpdateTime;
  }

  protected Resource(M manager, D data)
  {
    Manager = manager;
    Data = data;
  }

  public readonly M Manager;
  protected D Data { get; private set; }

  public bool IsValid => Manager.IsValid((R)this);
  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;
}

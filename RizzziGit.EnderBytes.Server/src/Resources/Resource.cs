using System.Data.Common;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Collections;
using Commons.Memory;
using Commons.Logging;

using Services;
using Newtonsoft.Json;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public delegate void ResourceInsertHandler(ResourceService.Transaction transaction, CancellationToken cancellationToken);
  public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, D oldData, CancellationToken cancellationTokens);
  public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction, CancellationToken cancellationToken);

  public abstract partial class ResourceManager(ResourceService service, string name, int version) : ResourceService.ResourceManager(service, name, version)
  {
    public delegate void ResourceInsertHandler(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken);
    public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, R resource, D oldData, CancellationToken cancellationToken);
    public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken);

    private readonly WeakDictionary<long, R> Resources = [];

    public event ResourceInsertHandler? ResourceInserted = null;
    public event ResourceUpdateHandler? ResourceUpdated;
    public event ResourceDeleteHandler? ResourceDeleted;

    public bool IsResourceValid(R resource)
    {
      lock (Resources)
      {
        lock (resource)
        {
          return Resources.TryGetValue(resource.Id, out R? testResource) && testResource == resource;
        }
      }
    }

    public void ThrowIfResourceInvalid(R resource)
    {
      if (!IsResourceValid(resource))
      {
        throw new ArgumentException("Invalid resource.", nameof(resource));
      }
    }

    protected abstract R NewResource(D data);
    private D CastToData(DbDataReader reader) => CastToData(reader,
      reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
    );
    protected abstract D CastToData(DbDataReader reader, long id, long createTime, long updateTime);

    protected R GetResource(D data)
    {
      lock (Resources)
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
    }

    protected R InsertAndGet(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default) => GetById(transaction, Insert(transaction, values, cancellationToken), cancellationToken);
    protected long Insert(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      values.Add(COLUMN_CREATE_TIME, insertTimestamp);
      values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

      List<object?> parameterList = [];

      return (long)(ulong)(SqlScalar(
        transaction,
        $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
        $"select last_insert_id();",
        [.. parameterList]
      ) ?? 0);
    }

    protected R? SelectFirst(ResourceService.Transaction transaction, WhereClause? where = null, OrderByClause? order = null, CancellationToken cancellationToken = default) => SelectOne(transaction, where, 0, order, cancellationToken);
    protected R? SelectOne(ResourceService.Transaction transaction, WhereClause? where = null, int? offset = null, OrderByClause? order = null, CancellationToken cancellationToken = default) => Select(transaction, where, new(1, offset), order, cancellationToken).FirstOrDefault();
    protected IEnumerable<R> Select(ResourceService.Transaction transaction, WhereClause? where = null, LimitClause? limit = null, OrderByClause? order = null, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      List<object?> parameterList = [];
      foreach (D data in SqlEnumeratedQuery(
        transaction,
        castToData,
        $"select * from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")}{(limit != null ? $" limit {limit.Apply()}" : "")}{(order != null ? $" order by {order.Apply()}" : "")};",
        [.. parameterList]
      ))
      {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Enumerated {Name} resource: #{data.Id}");
        yield return GetResource(data);
      }

      yield break;

      IEnumerable<D> castToData(DbDataReader reader)
      {
        while (reader.Read())
        {
          yield return CastToData(reader);
        }
      }
    }

    protected bool Exists(ResourceService.Transaction transaction, WhereClause? where = null, CancellationToken cancellationToken = default) => Count(transaction, where, cancellationToken) > 0;
    protected long Count(ResourceService.Transaction transaction, WhereClause? where = null, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      List<object?> parameterList = [];

      return (long)SqlScalar(
        transaction,
        $"select count(*) from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")};",
        [.. parameterList]
      )!;
    }

    protected long Delete(ResourceService.Transaction transaction, WhereClause where, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      List<object?> parameterList = [];

      string whereClause = where.Apply(parameterList);

      long count = 0;
      void log(long id) => Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Deleted {Name} resource: #{id}");
      Action? actions = null;

      SqlQuery(
        transaction,
        (reader) =>
        {
          if (ResourceDeleted == null)
          {
            while (reader.Read())
            {
              long id = reader.GetInt64(reader.GetOrdinal(COLUMN_ID));
              actions += () =>
              {
                if (Resources.TryGetValue(id, out R? resource))
                {
                  resource.Deleted?.Invoke(transaction, cancellationToken);
                }

                log(id);
              };

              count++;
            }
          }
          else
          {
            while (reader.Read())
            {
              D data = CastToData(reader);

              actions += !Resources.TryGetValue(data.Id, out R? resource)
              ? () =>
                {
                  resource = NewResource(data);
                  ResourceDeleted?.Invoke(transaction, resource, cancellationToken);
                  transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
                  log(data.Id);
                }
              : () =>
                {
                  resource.Deleted?.Invoke(transaction, cancellationToken);
                  ResourceDeleted?.Invoke(transaction, resource, cancellationToken);
                  Resources.Remove(data.Id);
                  transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
                  log(data.Id);
                };

              count++;
            }
          }
        },
        $"select * from {Name} where {whereClause};",
        [.. parameterList]
      );

      actions?.Invoke();

      return SqlNonQuery(transaction, $"delete from {Name} where {whereClause};", [.. parameterList]);
    }

    protected long Update(ResourceService.Transaction transaction, WhereClause where, SetClause set, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      List<object?> parameterList = [];
      set.Add(COLUMN_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

      string whereClause = where.Apply(parameterList);
      string setClause = set.Apply(parameterList);

      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      long count = 0;
      void log(long id) => Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Updated {Name} resource: #{id}");

      Action? actions = null;

      SqlQuery(
        transaction,
        (reader) =>
        {
          if (ResourceDeleted == null)
          {
            while (reader.Read())
            {
              D oldData = CastToData(reader);
              D newData() => SqlQuery(transaction, (reader) =>
              {
                reader.Read();
                return CastToData(reader);
              }, $"select * from {Name} where {COLUMN_ID} = {{0}};", oldData.Id);

              if (Resources.TryGetValue(oldData.Id, out R? resource))
              {
                actions += () =>
                {
                  resource.Data = newData();
                  transaction.RegisterOnFailureHandler(() => resource.Data = oldData);

                  resource.Updated?.Invoke(transaction, oldData, cancellationToken);
                  log(oldData.Id);
                };
              }

              count++;
            }

            return;
          }

          while (reader.Read())
          {
            D oldData = CastToData(reader);
            D newData() => SqlQuery(transaction, (reader) =>
            {
              reader.Read();
              return CastToData(reader);
            }, $"select * from {Name} where {COLUMN_ID} = {{0}};", oldData.Id);

            actions += Resources.TryGetValue(oldData.Id, out R? resource)
            ? () =>
            {
              resource.Data = newData();
              transaction.RegisterOnFailureHandler(() => resource.Data = oldData);

              ResourceUpdated?.Invoke(transaction, resource, oldData, cancellationToken);
              resource.Updated?.Invoke(transaction, oldData, cancellationToken);
              log(resource.Id);
            }
            : () =>
            {
              R resource = NewResource(newData());
              transaction.RegisterOnFailureHandler(() =>
              {
                resource.Data = oldData;
                Resources.Add(resource.Id, resource);
              });

              ResourceUpdated?.Invoke(transaction, resource, oldData, cancellationToken);
              resource.Updated?.Invoke(transaction, oldData, cancellationToken);
              log(resource.Id);
            };

            count++;
          }
        },
        $"create temporary table {temporaryTableName} as select * from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select * from {temporaryTableName}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      );

      actions?.Invoke();

      return count;
    }

    protected bool Update(ResourceService.Transaction transaction, R resource, SetClause set, CancellationToken cancellationToken = default)
    {
      lock (resource)
      {
        resource.ThrowIfInvalid();

        return Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set, cancellationToken) != 0;
      }
    }

    public virtual R GetById(ResourceService.Transaction transaction, long Id, CancellationToken cancellationToken = default) => Resources.TryGetValue(Id, out R? resource) ? resource : Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken).First();
    public virtual bool TryGetById(ResourceService.Transaction transaction, long Id, [NotNullWhen(true)] out R? resource, CancellationToken cancellationToken = default) => Resources.TryGetValue(Id, out resource) || ((resource = Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken).FirstOrDefault()) != null);

    public virtual bool Delete(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken = default)
    {
      lock (resource)
      {
        resource.ThrowIfInvalid();

        return Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), cancellationToken) != 0;
      }
    }
  }

  public abstract partial record ResourceData(long Id, long CreateTime, long UpdateTime);

  [JsonIgnore]
  public readonly M Manager = manager;
  [JsonIgnore]
  protected D Data { get; private set; } = data;

  public event ResourceUpdateHandler? Updated;
  public event ResourceDeleteHandler? Deleted;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;

  [JsonIgnore]
  public bool IsValid => Manager.IsResourceValid((R)this);
  public void ThrowIfInvalid() => Manager.ThrowIfResourceInvalid((R)this);

  public override string ToString() => Data.ToString();
}

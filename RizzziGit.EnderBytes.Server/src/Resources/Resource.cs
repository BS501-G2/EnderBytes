using System.Data.Common;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Framework.Memory;
using Framework.Logging;

using DatabaseWrappers;
using Services;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public delegate void ResourceEventHandler(ResourceService.Transaction transaction, CancellationToken cancellationToken);

  public abstract partial class ResourceManager(ResourceService service, string name, int version) : ResourceService.ResourceManager(service, name, version)
  {
    public delegate void ResourceEventHandler(ResourceService.Transaction transaction, long resourceId, CancellationToken cancellationToken);

    private readonly WeakDictionary<long, R> Resources = [];

    public event ResourceEventHandler? ResourceInserted = null;
    public event ResourceEventHandler? ResourceUpdated;
    public event ResourceEventHandler? ResourceDeleted;

    public bool IsResourceValid(R resource)
    {
      lock (resource)
      {
        return Resources.TryGetValue(resource.Id, out R? testResource) && testResource == resource;
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

    protected R InsertAndGet(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default) => GetById(transaction, Insert(transaction, values, cancellationToken), cancellationToken);
    protected long Insert(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      values.Add(COLUMN_CREATE_TIME, insertTimestamp);
      values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

      List<object?> parameterList = [];

      long id = (long)(ulong)(SqlScalar(
        transaction,
        $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
        $"select last_insert_id();",
        [.. parameterList]
      ) ?? -1);

      ResourceInserted?.Invoke(transaction, id, cancellationToken);
      return id;
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
      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      long count = 0;

      Action? actions = null;

      foreach (long id in SqlEnumeratedQuery(
        transaction,
        castToData,
        $"select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"delete from {Name} where {whereClause};",
        [.. parameterList]
      ))
      {
        actions += () =>
        {
          cancellationToken.ThrowIfCancellationRequested();

          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Deleted {Name} resource: #{id}");
          if (Resources.TryGetValue(id, out R? resource))
          {
            transaction.RegisterOnFailureHandler(() => Resources.Add(id, resource));
            Resources.Remove(id);

            ResourceDeleted?.Invoke(transaction, id, cancellationToken);
            resource.Deleted?.Invoke(transaction, cancellationToken);
            return;
          }

          ResourceDeleted?.Invoke(transaction, id, cancellationToken);
        };

        count++;
      }

      actions?.Invoke();
      return count;

      static IEnumerable<long> castToData(DbDataReader reader)
      {
        while (reader.Read())
        {
          yield return reader.GetInt64(reader.GetOrdinal(COLUMN_ID));
        }
      }
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

      Action? actions = null;
      foreach (long id in SqlEnumeratedQuery(
        transaction,
        castToData,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select * from {temporaryTableName}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      ))
      {
        actions += () =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Updated {Name} resource: #{id}");
          if (Resources.TryGetValue(id, out R? resource))
          {
            D oldData = resource.Data;

            transaction.RegisterOnFailureHandler(() => resource.Data = oldData);
            _ = GetById(transaction, id, cancellationToken);

            ResourceUpdated?.Invoke(transaction, id, cancellationToken);
            resource.Updated?.Invoke(transaction, cancellationToken);
          }
          else if (ResourceUpdated != null)
          {
            ResourceUpdated.Invoke(transaction, id, cancellationToken);
          }
        };

        count++;
      }

      actions?.Invoke();
      return count;

      static IEnumerable<long> castToData(DbDataReader reader)
      {
        while (reader.Read())
        {
          yield return reader.GetInt64(reader.GetOrdinal(COLUMN_ID));
        }
      }
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

  public readonly M Manager = manager;
  protected D Data { get; private set; } = data;

  public event ResourceEventHandler? Updated;
  public event ResourceEventHandler? Deleted;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;

  public bool IsValid => Manager.IsResourceValid((R)this);
  public void ThrowIfInvalid() => Manager.ThrowIfResourceInvalid((R)this);

  public override string ToString() => Data.ToString();
}

using System.Data.SQLite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Framework.Memory;
using Framework.Logging;

using Services;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction);
  public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, D oldData);

  public abstract partial class ResourceManager(ResourceService service, string name, int version) : ResourceService.ResourceManager(service, name, version)
  {
    public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction, R resource);
    public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, R resource, D oldData);
    public delegate void ResourceInsertHandler(ResourceService.Transaction transaction, R resource);

    private readonly WeakDictionary<long, R> Resources = [];

    public event ResourceInsertHandler? ResourceInserted;
    public event ResourceUpdateHandler? ResourceUpdated;
    public event ResourceDeleteHandler? ResourceDeleted;

    public bool IsResourceValid(R resource)
    {
      lock (this)
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
    private D CastToData(SQLiteDataReader reader) => CastToData(reader,
      reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
    );
    protected abstract D CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime);
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

    protected R Insert(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      values.Add(COLUMN_CREATE_TIME, insertTimestamp);
      values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

      List<object?> parameterList = [];
      return SqlQuery(
        transaction,
        (reader) =>
        {
          cancellationToken.ThrowIfCancellationRequested();

          reader.Read();
          R resource = GetResource(CastToData(reader));

          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] New {Name} resource: #{resource.Id}");

          transaction.RegisterOnFailureHandler(() => Resources.Remove(resource.Id));
          ResourceInserted?.Invoke(transaction, resource);
          return resource;
        },
        $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
        $"select * from {Name} where {COLUMN_ID} = last_insert_rowid() limit 1;",
        [.. parameterList]
      );
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

      IEnumerable<D> castToData(SQLiteDataReader reader)
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
      foreach (D data in SqlEnumeratedQuery(
        transaction,
        castToData,
        $"create temporary table {temporaryTableName} as select * from {Name} where {whereClause}; " +
        $"delete from {Name} where {whereClause}; " +
        $"select * from {temporaryTableName}; " +
        $"drop table {temporaryTableName}; ",
        [.. parameterList]
      ))
      {
        cancellationToken.ThrowIfCancellationRequested();

        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Deleted {Name} resource: #{data.Id}");
        if (Resources.TryGetValue(data.Id, out R? resource))
        {
          transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
          Resources.Remove(data.Id);
          ResourceDeleted?.Invoke(transaction, resource);
          resource.Deleted?.Invoke(transaction);
          continue;
        }

        resource = NewResource(data);
        transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
        ResourceDeleted?.Invoke(transaction, resource);

        count++;
      }

      return count;

      IEnumerable<D> castToData(SQLiteDataReader reader)
      {
        while (reader.Read())
        {
          yield return CastToData(reader);
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
      foreach (D newData in SqlEnumeratedQuery(
        transaction,
        castToData,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select {Name}.* from {temporaryTableName} left join (select * from {Name}) {Name} on {temporaryTableName}.{COLUMN_ID} = {Name}.{COLUMN_ID}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      ))
      {
        cancellationToken.ThrowIfCancellationRequested();
        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Updated {Name} resource: #{newData.Id}");
        if (Resources.TryGetValue(newData.Id, out R? resource))
        {
          D oldData = resource.Data;

          transaction.RegisterOnFailureHandler(() => resource.Data = oldData);
          resource.Data = newData;

          ResourceUpdated?.Invoke(transaction, resource, oldData);
          resource.Updated?.Invoke(transaction, oldData);
        }
        else if (ResourceUpdated != null)
        {
          resource = GetResource(newData);

          transaction.RegisterOnFailureHandler(() => Resources.Remove(resource.Id));
          ResourceUpdated.Invoke(transaction, resource, newData);
        }

        count++;
      }

      return count;

      IEnumerable<D> castToData(SQLiteDataReader reader)
      {
        while (reader.Read())
        {
          yield return CastToData(reader);
        }
      }
    }

    protected bool Update(ResourceService.Transaction transaction, R resource, SetClause set, CancellationToken cancellationToken = default) => Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set, cancellationToken) != 0;

    public virtual R? GetById(ResourceService.Transaction transaction, long Id, CancellationToken cancellationToken = default) => Resources.TryGetValue(Id, out R? resource) ? resource : Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken).FirstOrDefault();
    public virtual bool Delete(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken = default) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), cancellationToken) != 0;
  }

  public abstract partial record ResourceData(long Id, long CreateTime, long UpdateTime);

  public readonly M Manager = manager;
  protected D Data { get; private set; } = data;

  public event ResourceUpdateHandler? Updated;
  public event ResourceDeleteHandler? Deleted;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;

  public bool IsValid => Manager.IsResourceValid((R)this);
  public void ThrowIfInvalid() => Manager.ThrowIfResourceInvalid((R)this);
}

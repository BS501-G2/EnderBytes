using System.Data.SQLite;
using System.Text;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Framework.Memory;
using Framework.Logging;

using Services;
using Utilities;

public abstract partial class Resource<M, D, R>(M manager, D data)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction, R resource);
  public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, R resource, D oldData);
  public delegate void ResourceInsertHandler(ResourceService.Transaction transaction, R resource);

  public abstract partial class ResourceManager(ResourceService service, ResourceService.Scope scope, string name, int version) : ResourceService.ResourceManager(service, scope, name, version)
  {
    private readonly WeakDictionary<long, R> Resources = [];

    public event ResourceInsertHandler? ResourceInserted;
    public event ResourceUpdateHandler? ResourceUpdated;
    public event ResourceDeleteHandler? ResourceDeleted;

    public bool IsValid(R resource) => Resources.TryGetValue(resource.Id, out R? testResource) && testResource == resource;
    public void ThrowIfInvalid(R resource)
    {
      if (!IsValid(resource))
      {
        throw new InvalidOperationException("Invalid resource.");
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

    protected R Insert(ResourceService.Transaction transaction, ValueClause values)
    {
      ThrowIfInvalidScope(transaction);

      long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      values.Add(COLUMN_CREATE_TIME, insertTimestamp);
      values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

      List<object?> parameterList = [];
      return SqlQuery(
        transaction,
        (reader) =>
        {
          if (reader.Read())
          {
            R resource = GetResource(CastToData(reader));

            Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id} on {Scope}] New {Name} resource: #{resource.Id}");

            transaction.RegisterOnFailureHandler(() => Resources.Remove(resource.Id));
            ResourceInserted?.Invoke(transaction, resource);
            return resource;
          }

          throw new InvalidOperationException("Failed to get the new inserted row.");
        },
        $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
        $"select * from {Name} where {COLUMN_ID} = last_insert_rowid() limit 1;",
        [.. parameterList]
      );
    }

    protected R? SelectFirst(ResourceService.Transaction transaction, WhereClause? where = null, OrderByClause? order = null) => SelectOne(transaction, where, 0, order);
    protected R? SelectOne(ResourceService.Transaction transaction, WhereClause? where = null, int? offset = null, OrderByClause? order = null) => Select(transaction, where, new(1, offset), order).FirstOrDefault();
    protected IEnumerable<R> Select(ResourceService.Transaction transaction, WhereClause? where = null, LimitClause? limit = null, OrderByClause? order = null)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];
      foreach (D data in SqlEnumeratedQuery<D>(
        transaction,
        EnumerateReaderAndCastToData,
        $"select * from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")}{(limit != null ? $" limit {limit.Apply()}" : "")}{(order != null ? $" order by {order.Apply()}" : "")};",
        [.. parameterList]
      ))
      {
        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id} on {Scope}] Enumerated {Name} resource: #{data.Id}");
        yield return GetResource(data);
      }
    }

    protected bool Exists(ResourceService.Transaction transaction, WhereClause? where = null) => Count(transaction, where) > 0;
    protected long Count(ResourceService.Transaction transaction, WhereClause? where = null)
    {
      ThrowIfInvalidScope(transaction);
      List<object?> parameterList = [];

      return (long)(SqlScalar(
        transaction,
        $"select count(*) from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")};",
        [.. parameterList]
      ) ?? throw new InvalidOperationException("Failed to count rows."));
    }

    protected long Delete(ResourceService.Transaction transaction, WhereClause where)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];

      string whereClause = where.Apply(parameterList);

      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      long count = 0;
      if (ResourceDeleted != null)
      {
        foreach (D data in SqlEnumeratedQuery<D>(
          transaction,
          EnumerateReaderAndCastToData,
          $"create temporary table {temporaryTableName} as select * from {Name} where {whereClause}; " +
          $"delete from {Name} where {whereClause}; " +
          $"select * from {temporaryTableName}; " +
          $"drop table {temporaryTableName}; ",
          [.. parameterList]
        ))
        {
          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id} on {Scope}] Deleted {Name} resource: #{data.Id}");
          if (Resources.TryGetValue(data.Id, out R? resource))
          {
            transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
            Resources.Remove(data.Id);
            ResourceDeleted?.Invoke(transaction, resource);
            continue;
          }

          resource = NewResource(data);
          transaction.RegisterOnFailureHandler(() => Resources.Add(data.Id, resource));
          ResourceDeleted?.Invoke(transaction, resource);
        }
      }
      else
      {
        foreach (long affectedId in SqlEnumeratedQuery(
          transaction,
          enumerateAffectedId,
          $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
          $"delete from {Name} where {whereClause}; " +
          $"select {COLUMN_ID} from {temporaryTableName}; " +
          $"drop table {temporaryTableName}; ",
          [.. parameterList]
        ))
        {
          Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id} on {Scope}] Deleted {Name} resource: #{affectedId}");
          if (Resources.TryGetValue(affectedId, out R? resource))
          {
            transaction.RegisterOnFailureHandler(() => Resources.Add(affectedId, resource));
            Resources.Remove(affectedId);
          }
        }

        static IEnumerable<long> enumerateAffectedId(SQLiteDataReader reader)
        {
          while (reader.Read())
          {
            yield return reader.GetInt64(reader.GetOrdinal(COLUMN_ID));
          }
        }
      }
      return count;
    }

    protected long Update(ResourceService.Transaction transaction, WhereClause where, SetClause set)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];
      set.Add(COLUMN_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

      string whereClause = where.Apply(parameterList);
      string setClause = set.Apply(parameterList);

      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      long count = 0;
      foreach (D newData in SqlEnumeratedQuery<D>(
        transaction,
        EnumerateReaderAndCastToData,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select {Name}.* from {temporaryTableName} left join (select * from {Name}) {Name} on {temporaryTableName}.{COLUMN_ID} = {Name}.{COLUMN_ID}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      ))
      {
        Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id} on {Scope}] Updated {Name} resource: #{newData.Id}");
        if (Resources.TryGetValue(newData.Id, out R? resource))
        {
          D oldData = resource.Data;

          transaction.RegisterOnFailureHandler(() => resource.Data = oldData);
          resource.Data = newData;

          ResourceUpdated?.Invoke(transaction, resource, oldData);
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
    }

    private IEnumerable<D> EnumerateReaderAndCastToData(SQLiteDataReader reader)
    {
      while (reader.Read())
      {
        yield return CastToData(reader);
      }
    }

    protected bool Update(ResourceService.Transaction transaction, R resource, SetClause set) => Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set) != 0;

    public virtual R? GetById(ResourceService.Transaction transaction, long Id) => Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id)).FirstOrDefault();
    public virtual bool Delete(ResourceService.Transaction transaction, R resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id)) != 0;
  }

  public abstract partial record ResourceData(long Id, long CreateTime, long UpdateTime);

  public readonly M Manager = manager;
  protected D Data { get; private set; } = data;

  public long Id => Data.Id;
  public long CreateTime => Data.CreateTime;
  public long UpdateTime => Data.UpdateTime;

  public bool IsValid => Manager.IsValid((R)this);
  public void ThrowIfInalid() => Manager.ThrowIfInvalid((R)this);
}

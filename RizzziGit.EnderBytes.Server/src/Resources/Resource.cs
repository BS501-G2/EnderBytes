using System.Data.SQLite;
using System.Text;
using System.Security.Cryptography;

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
    public delegate void ResourceDeleteHandler(R resource);
    public delegate void ResourceUpdateHandler(R resource, D oldData);
    public delegate void ResourceInsertHandler(R resource);

    private readonly WeakDictionary<long, R> Resources = [];

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
      using SQLiteDataReader selectReader = SqlQuery(
        transaction,
        $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
        $"select * from {Name} where {COLUMN_ID} = last_insert_rowid() limit 1;",
        [.. parameterList]
      );

      if (selectReader.Read())
      {
        R resource = GetResource(CastToData(selectReader));

        transaction.RegisterOnFailureHandler(() => Resources.Remove(resource.Id));
        return resource;
      }

      throw new InvalidOperationException("Failed to get the new inserted row.");
    }

    protected R? SelectFirst(ResourceService.Transaction transaction, WhereClause? where = null, OrderByClause? order = null) => SelectOne(transaction, where, 0, order);
    protected R? SelectOne(ResourceService.Transaction transaction, WhereClause? where = null, int? offset = null, OrderByClause? order = null) => Select(transaction, where, new(1, offset), order).FirstOrDefault();
    protected IEnumerable<R> Select(ResourceService.Transaction transaction, WhereClause? where = null, LimitClause? limit = null, OrderByClause? order = null)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];
      using SQLiteDataReader reader = SqlQuery(
        transaction,
        $"select * from {Name}{
          (where != null ? $" where {where.Apply(parameterList)}" : "")
        }{
          (limit != null ? $" limit {limit.Apply()}" : "")
        }{
          (order != null ? $" order by {order.Apply()}" : "")
        };",
        [.. parameterList]
      );

      while (true)
      {
        yield return GetResource(CastToData(reader));
      }
    }

    protected bool Exists(ResourceService.Transaction transaction, WhereClause? where = null) => Count(transaction, where) > 0;
    protected long Count(ResourceService.Transaction transaction, WhereClause? where = null)
    {
      ThrowIfInvalidScope(transaction);
      List<object?> parameterList = [];

      return (long)(SqlScalar(
        transaction,
        $"select count(*) from {Name}{
          (where != null ? $" where {where.Apply(parameterList)}" : "")
        };",
        [.. parameterList]
      ) ?? throw new InvalidOperationException("Failed to count rows."));
    }

    protected long Delete(ResourceService.Transaction transaction, WhereClause where)
    {
      ThrowIfInvalidScope(transaction);

      List<object?> parameterList = [];

      string whereClause = where.Apply(parameterList);

      string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

      using SQLiteDataReader reader = SqlQuery(
        transaction,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"delete from {Name} where {whereClause}; " +
        $"select {COLUMN_ID} from {temporaryTableName}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      );

      long count = 0;
      while (reader.Read())
      {
        long affectedId = reader.GetInt64(reader.GetOrdinal(COLUMN_ID));

        if (Resources.TryGetValue(affectedId, out R? resource))
        {
          transaction.RegisterOnFailureHandler(() => Resources.Add(affectedId, resource));
          Resources.Remove(affectedId);
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
      foreach (D newData in CastToEnumerableData(SqlQuery(
        transaction,
        $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause}; " +
        $"update {Name} set {setClause} where {whereClause}; " +
        $"select {Name}.* from {temporaryTableName} left join (select * from {Name}) {Name} on {temporaryTableName}.{COLUMN_ID} = {Name}.{COLUMN_ID}; " +
        $"drop table {temporaryTableName};",
        [.. parameterList]
      )))
      {
        if (Resources.TryGetValue(newData.Id, out R? resource))
        {
          D oldData = resource.Data;

          transaction.RegisterOnFailureHandler(() => resource.Data = oldData);
          resource.Data = newData;
        }

        count++;
      }

      return count;
    }

    protected IEnumerable<D> CastToEnumerableData(SQLiteDataReader reader)
    {
      using (reader)
      {
        while (reader.Read())
        {
          yield return CastToData(reader);
        }
      }
    }

    public R? GetById(ResourceService.Transaction transaction, long Id) => Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id)).FirstOrDefault();
    public bool Delete(ResourceService.Transaction transaction, R resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id)) != 0;
    protected bool Update(ResourceService.Transaction transaction, R resource, SetClause set) => Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set) != 0;
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

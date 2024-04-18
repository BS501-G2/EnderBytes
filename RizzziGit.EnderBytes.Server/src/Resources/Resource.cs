using System.Data.Common;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;
using Commons.Logging;

using Services;

public abstract partial class ResourceManager<M, R, E>(ResourceService service, string name, int version) : ResourceService.ResourceManager(service, name, version)
    where M : ResourceManager<M, R, E>
    where R : ResourceManager<M, R, E>.Resource
    where E : ResourceService.Exception
{
  public abstract partial record Resource(long Id, long CreateTime, long UpdateTime);
  public sealed class NotFoundException(string name, long id) : ResourceService.Exception($"\"{name}\" resource #{id} does not exist.");

  public delegate void ResourceUpdateHandler(ResourceService.Transaction transaction, R resource, R oldResource, CancellationToken cancellationToken);
  public delegate void ResourceDeleteHandler(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken);

  public event ResourceUpdateHandler? ResourceUpdated;
  public event ResourceDeleteHandler? ResourceDeleted;

  private R ToResource(DbDataReader reader) => ToResource(reader,
    reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
  );
  protected abstract R ToResource(DbDataReader reader, long id, long createTime, long updateTime);

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
    foreach (R resource in SqlEnumeratedQuery(
      transaction,
      castToData,
      $"select * from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")}{(limit != null ? $" limit {limit.Apply()}" : "")}{(order != null ? $" order by {order.Apply()}" : "")};",
      [.. parameterList]
    ))
    {
      cancellationToken.ThrowIfCancellationRequested();
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Enumerated {Name} resource: #{resource.Id}");
      yield return resource;
    }

    yield break;

    IEnumerable<R> castToData(DbDataReader reader)
    {
      while (reader.Read())
      {
        yield return ToResource(reader);
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
            actions += () => log(id);

            count++;
          }
        }
        else
        {
          while (reader.Read())
          {
            R resource = ToResource(reader);

            actions += () =>
            {
              ResourceDeleted?.Invoke(transaction, resource, cancellationToken);
              log(resource.Id);
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
        while (reader.Read())
        {
          R oldResource = ToResource(reader);
          R newResource() => SqlQuery(transaction, (reader) =>
          {
            reader.Read();
            return ToResource(reader);
          }, $"select * from {Name} where {COLUMN_ID} = {{0}};", oldResource.Id);

          actions += () =>
          {
            ResourceUpdated?.Invoke(transaction, newResource(), oldResource, cancellationToken);
            log(oldResource.Id);
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
    return Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set, cancellationToken) != 0;
  }

  public virtual R GetById(ResourceService.Transaction transaction, long Id, CancellationToken cancellationToken = default)
  {
    foreach (R resource in Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken)) {
      return resource;
    }

    throw new NotFoundException(Name, Id);
  }

  public virtual bool TryGetById(ResourceService.Transaction transaction, long Id, [NotNullWhen(true)] out R? resource, CancellationToken cancellationToken = default) => (resource = Select(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken).FirstOrDefault()) != null;

  public virtual bool Delete(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken = default)
  {
    return Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), cancellationToken) != 0;
  }
}

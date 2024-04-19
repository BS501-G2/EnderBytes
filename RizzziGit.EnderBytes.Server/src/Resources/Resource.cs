using System.Data.Common;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;
using Commons.Logging;

using Services;
using System.Runtime.CompilerServices;

public abstract partial class ResourceManager<M, R, E>(ResourceService service, string name, int version) : ResourceService.ResourceManager(service, name, version)
    where M : ResourceManager<M, R, E>
    where R : ResourceManager<M, R, E>.Resource
    where E : ResourceService.Exception
{
  public abstract partial record Resource(long Id, long CreateTime, long UpdateTime);
  public sealed class NotFoundException(string name, long? id) : ResourceService.Exception($"\"{name}\" resource #{id} does not exist.");
  public sealed class NoMatchException(string name) : ResourceService.Exception($"No \"{name}\" resource has matched the criteria.");

  public delegate Task ResourceUpdateHandler(ResourceService.Transaction transaction, R resource, R oldResource, CancellationToken cancellationToken);
  public delegate Task ResourceDeleteHandler(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken);

  private readonly List<ResourceUpdateHandler> ResourceUpdatedCallbacks = [];
  private readonly List<ResourceDeleteHandler> ResourceDeletedCallbacks = [];

  public void RegisterUpdateHandler(ResourceUpdateHandler handler)
  {
    lock (ResourceUpdatedCallbacks)
    {
      ResourceUpdatedCallbacks.Add(handler);
    }
  }

  public void RegisterDeleteHandler(ResourceDeleteHandler handler)
  {
    lock (ResourceDeletedCallbacks)
    {
      ResourceDeletedCallbacks.Add(handler);
    }
  }

  private R ToResource(DbDataReader reader) => ToResource(reader,
    reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
  );
  protected abstract R ToResource(DbDataReader reader, long id, long createTime, long updateTime);

  protected async Task<R> InsertAndGet(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default) => (await GetById(transaction, await Insert(transaction, values, cancellationToken), cancellationToken))!;
  protected async Task<long> Insert(ResourceService.Transaction transaction, ValueClause values, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    values.Add(COLUMN_CREATE_TIME, insertTimestamp);
    values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

    List<object?> parameterList = [];

    return (long)(ulong)((await SqlScalar(
      transaction,
      $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; " +
      $"select last_insert_id();",
      [.. parameterList]
    )) ?? 0);
  }

  protected async Task<R?> SelectFirst(ResourceService.Transaction transaction, WhereClause? where = null, OrderByClause? order = null, CancellationToken cancellationToken = default) => await SelectOne(transaction, where, 0, order, cancellationToken);
  protected async Task<R?> SelectOne(ResourceService.Transaction transaction, WhereClause? where = null, int? offset = null, OrderByClause? order = null, CancellationToken cancellationToken = default)
  {
    await foreach (R resource in Select(transaction, where, new(1, offset), order, cancellationToken))
    {
      return resource;
    }

    return null;
  }
  protected async IAsyncEnumerable<R> Select(ResourceService.Transaction transaction, WhereClause? where = null, LimitClause? limit = null, OrderByClause? order = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    List<object?> parameterList = [];
    await foreach (R resource in SqlQuery(
      transaction,
      (reader) => Task.FromResult(ToResource(reader)),
      $"select * from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")}{(limit != null ? $" limit {limit.Apply()}" : "")}{(order != null ? $" order by {order.Apply()}" : "")};",
      [.. parameterList]
    ))
    {
      cancellationToken.ThrowIfCancellationRequested();
      Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Enumerated {Name} resource: #{resource.Id}");
      yield return resource;
    }

    yield break;
  }

  protected async Task<bool> Exists(ResourceService.Transaction transaction, WhereClause? where = null, CancellationToken cancellationToken = default) => await Count(transaction, where, cancellationToken) > 0;
  protected async Task<long> Count(ResourceService.Transaction transaction, WhereClause? where = null, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    List<object?> parameterList = [];

    return (long)(await SqlScalar(
      transaction,
      $"select count(*) from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")};",
      [.. parameterList]
    ))!;
  }

  protected async Task<long> Delete(ResourceService.Transaction transaction, WhereClause where, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    List<object?> parameterList = [];

    string whereClause = where.Apply(parameterList);

    void log(long id) => Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Deleted {Name} resource: #{id}");
    foreach (Func<Task> callback in await SqlQuery(
      transaction,
      (reader) =>
      {
        R resource = ToResource(reader);
        log(resource.Id);

        return Task.FromResult(async () =>
        {
          List<ResourceDeleteHandler> handlers;

          lock (ResourceDeletedCallbacks)
          {
            handlers = [.. ResourceDeletedCallbacks];
          }

          foreach (ResourceDeleteHandler callback in handlers)
          {
            await callback(transaction, resource, cancellationToken);
          }
        });
      },
      $"select * from {Name} where {whereClause};",
      [.. parameterList]
    ).ToListAsync(cancellationToken))
    {
      await callback();
    }

    return await SqlNonQuery(transaction, $"delete from {Name} where {whereClause};", [.. parameterList]);
  }

  protected async Task<long> Update(ResourceService.Transaction transaction, WhereClause where, SetClause set, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    List<object?> parameterList = [];
    set.Add(COLUMN_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    string whereClause = where.Apply(parameterList);
    string setClause = set.Apply(parameterList);

    string temporaryTableName = $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

    void log(long id) => Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Updated {Name} resource: #{id}");

    long count = 0;
    foreach (Func<Task> callback in await SqlQuery(
      transaction,
      (reader) =>
      {
        R oldResource = ToResource(reader);
        async Task<R> getNewResource() => await SqlQuery(transaction, (reader) => Task.FromResult(ToResource(reader)), $"select * from {Name} where {COLUMN_ID} = {{0}};", oldResource.Id).FirstAsync(cancellationToken);

        count++;

        log(oldResource.Id);

        return Task.FromResult(async () =>
        {
          List<ResourceUpdateHandler> handlers;

          lock (ResourceUpdatedCallbacks)
          {
            handlers = [.. ResourceUpdatedCallbacks];
          }

          R newResource = await getNewResource();

          foreach (ResourceUpdateHandler callback in handlers)
          {
            await callback(transaction, oldResource, newResource, cancellationToken);
          }
        });
      },
      $"create temporary table {temporaryTableName} as select * from {Name} where {whereClause}; " +
      $"update {Name} set {setClause} where {whereClause}; " +
      $"select * from {temporaryTableName}; " +
      $"drop table {temporaryTableName};",
      [.. parameterList]
    ).ToListAsync(cancellationToken))
    {
      await callback();
    }

    return count;
  }

  protected async Task<bool> Update(ResourceService.Transaction transaction, R resource, SetClause set, CancellationToken cancellationToken = default)
  {
    return await Update(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), set, cancellationToken) != 0;
  }

  public virtual async Task<R?> GetById(ResourceService.Transaction transaction, long Id, CancellationToken cancellationToken = default)
  {
    return await SelectFirst(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken);
  }

  public virtual async Task<R> GetByRequiredId(ResourceService.Transaction transaction, long Id, CancellationToken cancellationToken = default)
  {
    return await SelectFirst(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id), cancellationToken: cancellationToken) ?? throw new NotFoundException(Name, Id);
  }

  public virtual async Task<bool> Delete(ResourceService.Transaction transaction, R resource, CancellationToken cancellationToken = default)
  {
    return await Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id), cancellationToken) != 0;
  }
}

using System.Data.Common;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Logging;
using Commons.Memory;
using Services;

public abstract partial class ResourceManager<M, R>(
    ResourceService service,
    string name,
    int version
) : ResourceService.ResourceManager(service, name, version)
    where M : ResourceManager<M, R>
    where R : ResourceManager<M, R>.Resource
{
    public abstract partial record Resource(long Id, long CreateTime, long UpdateTime);

    public delegate Task ResourceUpdateHandler(
        ResourceService.Transaction transaction,
        R resource,
        R oldResource
    );
    public delegate Task ResourceDeleteHandler(ResourceService.Transaction transaction, R resource);

    public T GetManager<T>()
        where T : ResourceService.ResourceManager => Service.GetManager<T>();

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

    private R ToResource(DbDataReader reader) =>
        ToResource(
            reader,
            reader.GetInt64(reader.GetOrdinal(COLUMN_ID)),
            reader.GetInt64(reader.GetOrdinal(COLUMN_CREATE_TIME)),
            reader.GetInt64(reader.GetOrdinal(COLUMN_UPDATE_TIME))
        );

    protected abstract R ToResource(DbDataReader reader, long id, long createTime, long updateTime);

    protected async Task<R> InsertAndGet(
        ResourceService.Transaction transaction,
        ValueClause values
    ) => (await GetById(transaction, await Insert(transaction, values)))!;

    protected async Task<long> Insert(ResourceService.Transaction transaction, ValueClause values)
    {
        long insertTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        values.Add(COLUMN_CREATE_TIME, insertTimestamp);
        values.Add(COLUMN_UPDATE_TIME, insertTimestamp);

        List<object?> parameterList = [];

        return (long)(
            (
                await SqlScalar(
                    transaction,
                    $"insert into {Name} ({string.Join(", ", values.Keys)}) values {values.Apply(parameterList)}; "
                        + $"select last_insert_rowid();",
                    [.. parameterList]
                )
            ) ?? 0
        );
    }

    protected async Task<R?> SelectFirst(
        ResourceService.Transaction transaction,
        WhereClause? where = null,
        OrderByClause[]? order = null
    ) => await SelectOne(transaction, where, 0, order);

    protected async Task<R?> SelectOne(
        ResourceService.Transaction transaction,
        WhereClause? where = null,
        long? offset = null,
        OrderByClause[]? order = null
    )
    {
        foreach (R resource in await Select(transaction, where, new(1, offset), order))
        {
            return resource;
        }

        return null;
    }

    protected async Task<R[]> Select(
        ResourceService.Transaction transaction,
        WhereClause? where = null,
        LimitClause? limit = null,
        OrderByClause?[]? order = null
    )
    {
        List<object?> parameterList = [];
        List<R> resources = [];

        foreach (
            R resource in await SqlQuery(
                transaction,
                (reader) => Task.FromResult(ToResource(reader)),
                $"select * from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")}{(order != null ? $" order by {string.Join(", ", order.Where((clause) => clause != null).Select((clause) => clause!.Apply()))}" : "")}{(limit != null ? $" limit {limit.Apply()}" : "")};",
                [.. parameterList]
            )
        )
        {
            Logger.Log(
                LogLevel.Debug,
                $"[Transaction #{transaction.Id}] Enumerated {Name} resource: #{resource.Id}"
            );

            resources.Add(resource);
        }

        return [.. resources];
    }

    protected async Task<bool> Exists(
        ResourceService.Transaction transaction,
        WhereClause? where = null
    ) => await Count(transaction, where) > 0;

    protected async Task<long> Count(
        ResourceService.Transaction transaction,
        WhereClause? where = null
    )
    {
        List<object?> parameterList = [];

        return (long?)
                await SqlScalar(
                    transaction,
                    $"select count(*) from {Name}{(where != null ? $" where {where.Apply(parameterList)}" : "")};",
                    [.. parameterList]
                ) ?? 0;
    }

    protected async Task<long> Delete(ResourceService.Transaction transaction, WhereClause where)
    {
        List<object?> parameterList = [];

        string whereClause = where.Apply(parameterList);

        void log(long id) =>
            Logger.Log(
                LogLevel.Debug,
                $"[Transaction #{transaction.Id}] Deleted {Name} resource: #{id}"
            );
        long count = 0;

        string temporaryTableName =
            $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

        await SqlNonQuery(
            transaction,
            $"create temporary table {temporaryTableName} as select * from {Name} where {whereClause};",
            [.. parameterList]
        );

        await SqlNonQuery(
            transaction,
            $"delete from {Name} where {whereClause};",
            [.. parameterList]
        );

        foreach (
            Func<Task> callback in await SqlQuery(
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
                            await callback(transaction, resource);
                        }
                    });
                },
                $"select * from {temporaryTableName};",
                [.. parameterList]
            )
        )
        {
            await callback();
            count++;
        }

        await SqlNonQuery(transaction, $"drop table {temporaryTableName};", [.. parameterList]);

        return count;
    }

    protected async Task<long> Update(
        ResourceService.Transaction transaction,
        WhereClause where,
        SetClause set
    )
    {
        List<object?> parameterList = [];
        set.TryAdd(COLUMN_UPDATE_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        string whereClause = where.Apply(parameterList);
        string setClause = set.Apply(parameterList);

        string temporaryTableName =
            $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

        void log(long id) =>
            Logger.Log(
                LogLevel.Debug,
                $"[Transaction #{transaction.Id}] Updated {Name} resource: #{id}"
            );

        long count = 0;

        await SqlNonQuery(
            transaction,
            $"create temporary table {temporaryTableName} as select {COLUMN_ID} from {Name} where {whereClause};",
            [.. parameterList]
        );

        await SqlNonQuery(
            transaction,
            $"update {Name} set {setClause} where {whereClause};",
            [.. parameterList]
        );

        foreach (
            Func<Task> callback in await SqlQuery(
                transaction,
                (reader) =>
                {
                    R oldResource = ToResource(reader);
                    async Task<R> getNewResource() =>
                        (
                            await SqlQuery(
                                transaction,
                                (reader) => Task.FromResult(ToResource(reader)),
                                $"select * from {Name} where {COLUMN_ID} = {{0}};",
                                oldResource.Id
                            )
                        ).First();

                    log(oldResource.Id);

                    string temporaryTableName =
                        $"Temp_{((CompositeBuffer)RandomNumberGenerator.GetBytes(8)).ToHexString()}";

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
                            await callback(transaction, oldResource, newResource);
                        }
                    });
                },
                $"select {Name}.* from {temporaryTableName} left join (select * from {Name}) {Name} on {temporaryTableName}.{COLUMN_ID} = {Name}.{COLUMN_ID};",
                [.. parameterList]
            )
        )
        {
            await callback();
            count++;
        }

        await SqlNonQuery(transaction, $"drop table {temporaryTableName};", [.. parameterList]);

        return count;
    }

    protected async Task<bool> Update(
        ResourceService.Transaction transaction,
        R resource,
        SetClause set
    )
    {
        return await Update(
                transaction,
                new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id),
                set
            ) != 0;
    }

    public virtual async Task<R?> GetById(ResourceService.Transaction transaction, long Id)
    {
        return await SelectFirst(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id));
    }

    public virtual async Task<R> GetByRequiredId(ResourceService.Transaction transaction, long Id)
    {
        return await SelectFirst(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", Id))
            ?? throw new NotFoundException(Name, Id);
    }

    public virtual async Task<bool> Delete(ResourceService.Transaction transaction, R resource)
    {
        return await Delete(transaction, new WhereClause.CompareColumn(COLUMN_ID, "=", resource.Id))
            != 0;
    }
}

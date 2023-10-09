using System.Data.SQLite;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using RizzziGit.Buffer;

using WhereClause = Dictionary<string, (string condition, object? value, string? collate)>;
using ValueClause = Dictionary<string, object?>;

public abstract class Resource<M, D, R> : Shared.Resources.Resource<M, D, R>, Shared.Resources.IResource
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  protected const string KEY_ID = "ID";
  protected const string KEY_CREATE_TIME = "CreateTime";
  protected const string KEY_UPDATE_TIME = "UpdateTime";

  private const string RV_TABLE = "__TableVersions";
  private const string RV_COLUMN_NAME = "Name";
  private const string RV_COLUMN_VERSION = "Version";

  public new abstract class ResourceData : Shared.Resources.Resource<M, D, R>.ResourceData, Shared.Resources.IResourceData
  {
    protected ResourceData(ulong id, long createTime, long updateTime) : base(id, createTime, updateTime)
    {
    }
  }

  public new abstract class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    public sealed class ResourceEnumerator(Resource<M, D, R>.ResourceManager.ResourceStream collection, CancellationToken cancellationToken) : ResourceEnumerator<ResourceStream, ResourceEnumerator>(collection, cancellationToken)
    {
      public override R Current => Stream.GetCurrent();
      public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
      public override ValueTask<bool> MoveNextAsync() => new(Stream.MoveNext(CancellationToken));
    }

    public sealed class ResourceStream(M manager, SQLiteDataReader reader) : ResourceStream<ResourceStream, ResourceEnumerator>(manager), IAsyncDisposable
    {
      public new readonly M Manager = manager;
      public readonly SQLiteDataReader Reader = reader;
      public override ValueTask DisposeAsync() => Reader.DisposeAsync();
      public override R GetCurrent() => Manager.AsResource(Manager.CreateData(Reader));
      public override Task<bool> MoveNext(CancellationToken cancellationToken) => Reader.ReadAsync(cancellationToken);
      protected override ResourceEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default) => new(this, cancellationToken);

      public async Task<List<R>> ToList(CancellationToken cancellationToken)
      {
        List<R> list = [];

        while (await MoveNext(cancellationToken))
        {
          list.Add(GetCurrent());
        }

        await DisposeAsync();
        return list;
      }
    }

    private static Task<int> InitVersioningTable(SQLiteConnection connection, CancellationToken cancellationToken) => connection.ExecuteNonQueryAsync(@$"create table if not exists {RV_TABLE}({RV_COLUMN_NAME} varchar(128) primary key, {RV_COLUMN_VERSION} integer not null);", cancellationToken);
    private static async Task SetResourceVersion(SQLiteConnection connection, string name, int version, CancellationToken cancellationToken)
    {
      if ((await connection.ExecuteNonQueryAsync($"update {RV_TABLE} set {RV_COLUMN_VERSION} = {{1}} where {RV_COLUMN_NAME} = {{0}}", cancellationToken, name, version)) == 0)
      {
        await connection.ExecuteNonQueryAsync($"insert into {RV_TABLE} ({RV_COLUMN_NAME},{RV_COLUMN_VERSION}) values ({{0}}, {{1}});", cancellationToken, name, version);
      }
    }
    private static async Task<int?> GetResourceVersion(SQLiteConnection connection, string name, CancellationToken cancellationToken)
    {
      SQLiteDataReader reader = await connection.ExecuteReaderAsync(@$"select * from {RV_TABLE} where {RV_COLUMN_NAME} = {{0}} limit 1;", cancellationToken, name);
      return reader.Read() ? (int)(long)reader[RV_COLUMN_VERSION] : null;
    }

    protected ResourceManager(MainResourceManager main, int version, string name) : base(main, version, name)
    {
      Main = main;
      Logger = new(name);

      Main.Logger.Subscribe(Logger);

      ResourceDeleteListeners = [];
      ResourceUpdateListeners = [];
      ResourceCreateListeners = [];
    }

    public new readonly MainResourceManager Main;
    public readonly Logger Logger;

    public delegate Task ResourceEventListener(SQLiteConnection connection, R resource, CancellationToken cancellationToken);
    public readonly List<ResourceEventListener> ResourceDeleteListeners;
    public readonly List<ResourceEventListener> ResourceUpdateListeners;
    public readonly List<ResourceEventListener> ResourceCreateListeners;

    public Database Database => Main.RequireDatabase();

    protected long GenerateTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    protected async Task<R> DbInsert(SQLiteConnection connection, ValueClause data, CancellationToken cancellationToken)
    {
      long timestamp = GenerateTimestamp();

      data.Add(KEY_CREATE_TIME, timestamp);
      data.Add(KEY_UPDATE_TIME, timestamp);

      ulong newId = await Database.Insert(connection, Name, data, cancellationToken);

      await using var a = await DbSelect(connection, new() { { KEY_ID, ("=", newId, null) } }, null, null, cancellationToken);
      await foreach (R resource in a)
      {
        Database.RegisterOnTransactionCompleteHandlers(null, (_) =>
        {
          RemoveFromMemory(resource.ID);
          resource.IsDeleted = true;
          Logger.Log(Logger.LOGLEVEL_WARN, $"#{resource.ID} removed from memory due to failed transaction.");

          return Task.CompletedTask;
        });

        Logger.Log(Logger.LOGLEVEL_VERBOSE, $"#{resource.ID} inserted to the database.");

        foreach (ResourceEventListener handle in ResourceCreateListeners)
        {
          await handle(connection, resource, cancellationToken);
        }
        return resource;
      }

      throw new InvalidOperationException("Failed to get new inserted resource. Possibly a race condition.");
    }

    protected async Task<ResourceStream> DbSelect(SQLiteConnection connection, WhereClause where, (int? offset, int length)? limit, (string column, string orderBy)? order, CancellationToken cancellationToken) => new ResourceStream((M)this, await Database.Select(connection, Name, where, limit, order, cancellationToken));
    protected async Task<R?> DbSelectOne(SQLiteConnection connection, WhereClause where, int? offset, (string column, string orderBy)? order, CancellationToken cancellationToken)
    {
      await using var stream = await DbSelect(connection, where, (offset, 1), order, cancellationToken);
      await foreach (R resource in stream)
      {
        return resource;
      }

      return null;
    }

    protected async Task<bool> DbUpdate(SQLiteConnection connection, WhereClause where, ValueClause newData, CancellationToken cancellationToken)
    {
      newData.TryAdd(KEY_UPDATE_TIME, GenerateTimestamp());

      int output = 0;
      await using var stream = await DbSelect(connection, where, null, null, cancellationToken);
      await foreach (R resource in stream)
      {
        D oldData = resource.Data;

        Database.RegisterOnTransactionCompleteHandlers(null, (connection) =>
        {
          resource.UpdateData(oldData);

          return Task.CompletedTask;
        });

        if (!await Database.Update(connection, Name, new() { { KEY_ID, ("=", resource.ID, null) } }, newData, cancellationToken))
        {
          continue;
        }

        Logger.Log(Logger.LOGLEVEL_VERBOSE, $"#{resource.ID} updated on the database.");
        await GetByID(connection, resource.ID, cancellationToken);
        foreach (ResourceEventListener handle in ResourceUpdateListeners)
        {
          await handle(connection, resource, cancellationToken);
        }
        output++;
      }

      return output != 0;
    }

    protected virtual async Task<bool> DbDelete(SQLiteConnection connection, WhereClause data, CancellationToken cancellationToken)
    {
      int output = 0;

      await using var stream = await DbSelect(connection, data, null, null, cancellationToken);
      await foreach (R resource in stream)
      {
        if (!await Delete(connection, resource, cancellationToken))
        {
          continue;
        }

        output++;
      }

      return output != 0;
    }

    public Task<R?> GetByID(SQLiteConnection connection, string idHex, CancellationToken cancellationToken) => GetByID(connection, Buffer.From(idHex, StringEncoding.Hex).ToUInt64(), cancellationToken);
    public Task<R?> GetByID(SQLiteConnection connection, ulong id, CancellationToken cancellationToken) => DbSelectOne(connection, new() { { KEY_ID, ("=", id, null) } }, null, null, cancellationToken);

    public override Task Init(CancellationToken cancellationToken) => Database.RunTransaction(async (connection, cancellationToken) =>
    {
      await InitVersioningTable(connection, cancellationToken);

      int? version = await GetResourceVersion(connection, Name, cancellationToken);
      if (version == null)
      {
        await connection.ExecuteNonQueryAsync(@$"create table {Name}(
          {KEY_ID} integer primary key autoincrement,
          {KEY_CREATE_TIME} integer not null,
          {KEY_UPDATE_TIME} integer not null
        );", cancellationToken);

        await OnInit(connection, cancellationToken);
      }
      else
      {
        await OnInit(connection, (int)version, cancellationToken);
      }

      await SetResourceVersion(connection, Name, Version, cancellationToken);
    }, cancellationToken);

    private D CreateData(SQLiteDataReader reader) => CreateData(reader,
      (ulong)(long)reader[KEY_ID],
      (long)reader[KEY_CREATE_TIME],
      (long)reader[KEY_UPDATE_TIME]
    );

    protected abstract D CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime);
    protected abstract Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken);
    protected abstract Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken);

    public virtual async Task<bool> Delete(SQLiteConnection connection, R resource, CancellationToken cancellationToken)
    {
      if (resource.IsDeleted)
      {
        return false;
      }

      foreach (ResourceEventListener handle in ResourceDeleteListeners)
      {
        await handle(connection, resource, cancellationToken);
      }

      RemoveFromMemory(resource.ID);
      resource.IsDeleted = true;
      if (!await Database.Delete(connection, Name, new() { { KEY_ID, ("=", resource.ID, null) } }, cancellationToken))
      {
        return false;
      }

      Database.RegisterOnTransactionCompleteHandlers(null, (connection) =>
      {
        PutToMemoryByID(resource);
        resource.IsDeleted = false;
        return Task.CompletedTask;
      });

      Logger.Log(Logger.LOGLEVEL_VERBOSE, $"#{resource.ID} deleted from the database.");
      return true;
    }
  }

  protected Resource(M manager, D data) : base(manager, data)
  {
    Logger = new(IDHex);
    IsDeleted = false;

    manager.Logger.Subscribe(Logger);
    Logger.Log(Logger.LOGLEVEL_VERBOSE, "Resource Retrieved");
  }

  ~Resource()
  {
    Logger.Log(Logger.LOGLEVEL_VERBOSE, "Resource Released");
  }

  public readonly Logger Logger;

  protected override void UpdateData(D data)
  {
    Logger.Log(Logger.LOGLEVEL_VERBOSE, "Resource Refreshed");
  }

  public bool IsDeleted { get; private set; }
  public string IDHex => Buffer.From(ID).ToHexString();
}

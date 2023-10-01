using System.Data.SQLite;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using RizzziGit.Buffer;

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
    protected ResourceData(ulong id, ulong createTime, ulong updateTime) : base(id, createTime, updateTime)
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
    }

    public sealed class DatabaseWrapper(M manager)
    {
      public readonly M Manager = manager;
      public Database Database => Manager.Database;

      public async Task<ulong> Insert(SQLiteConnection connection, Dictionary<string, object?> data, CancellationToken cancellationToken)
      {
        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"insert into {Manager.Name}");
          if (data.Count != 0)
          {
            lock (data)
            {
              commandStringBuilder.Append('(');

              for (int index = 0; index < data.Count; index++)
              {
                if (index != 0)
                {
                  commandStringBuilder.Append(',');
                }

                commandStringBuilder.Append(data.ElementAt(index).Key);
              }

              commandStringBuilder.Append($") values ({connection.ParamList(data.Count)});");
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. data.Values]);
        return (ulong)connection.LastInsertRowId;
      }

      public async Task<bool> Delete(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> where, CancellationToken cancellationToken)
      {
        List<object?> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"delete from {Manager.Name}");

          if (where.Count != 0)
          {
            commandStringBuilder.Append($" where ");

            for (int index = 0; index < where.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(" and ");
              }

              KeyValuePair<string, (string condition, object? value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
      }

      public async Task<R?> SelectOne(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> where, int? offset, CancellationToken cancellationToken)
      {
        await using var stream = await Select(connection, where, offset, 1, cancellationToken);
        await foreach (R resource in stream)
        {
          return resource;
        }

        return null;
      }

      public async Task<ResourceStream> Select(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> where, int? offset, int? length, CancellationToken cancellationToken)
      {
        List<object?> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"select * from {Manager.Name}");

          if (where.Count != 0)
          {
            commandStringBuilder.Append($" where ");

            for (int index = 0; index < where.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(" and ");
              }

              KeyValuePair<string, (string condition, object? value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          if (length != null)
          {
            if (offset != null)
            {
              commandStringBuilder.Append($" limit {offset} {length};");
            }
            else
            {
              commandStringBuilder.Append($" limit {length};");
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return new(Manager, await connection.ExecuteReaderAsync(commandString, cancellationToken, [.. parameters]));
      }

      public async Task<bool> Update(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> where, Dictionary<string, object?> data, CancellationToken cancellationToken)
      {
        if (data.Count == 0)
        {
          return false;
        }

        List<object?> parameters = [];

        string commandString;
        {
          StringBuilder commandStringBuilder = new();

          commandStringBuilder.Append($"update {Manager.Name}");
          if (data.Count != 0)
          {
            commandStringBuilder.Append(" set ");

            for (int index = 0; index < data.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(", ");
              }

              KeyValuePair<string, object?> dataEntry = data.ElementAt(index);
              commandStringBuilder.Append($"{dataEntry.Key} = ({{{parameters.Count}}})");
              parameters.Add(dataEntry.Value);
            }
          }

          if (where.Count != 0)
          {
            commandStringBuilder.Append($" where ");

            for (int index = 0; index < where.Count; index++)
            {
              if (index != 0)
              {
                commandStringBuilder.Append(" and ");
              }

              KeyValuePair<string, (string condition, object? value)> whereEntry = where.ElementAt(index);
              commandStringBuilder.Append($"{whereEntry.Key} {whereEntry.Value.condition} ({{{parameters.Count}}})");
              parameters.Add(whereEntry.Value.value);
            }
          }

          commandString = commandStringBuilder.ToString();
          commandStringBuilder.Clear();
        }

        return (await connection.ExecuteNonQueryAsync(commandString, cancellationToken, [.. parameters])) != 0;
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
      Wrapper = new((M)this);
      Logger = new(name);

      Main.Logger.Subscribe(Logger);

      ResourceDeleteHandles = [];
    }

    ~ResourceManager()
    {
      Main.Logger.Unsubscribe(Logger);
    }

    public new readonly MainResourceManager Main;
    public readonly DatabaseWrapper Wrapper;
    public readonly EnderBytesLogger Logger;

    public delegate Task ResourceDeleteHandle(SQLiteConnection connection, R resource, CancellationToken cancellationToken);
    public readonly List<ResourceDeleteHandle> ResourceDeleteHandles;

    public Database Database => Main.RequireDatabase();

    protected ulong GenerateTimestamp() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    protected async Task<R> DbInsert(SQLiteConnection connection, Dictionary<string, object?> data, CancellationToken cancellationToken)
    {
      ulong timestamp = GenerateTimestamp();

      data.Add(KEY_CREATE_TIME, timestamp);
      data.Add(KEY_UPDATE_TIME, timestamp);

      ulong newId = await Wrapper.Insert(connection, data, cancellationToken);

      await using var a = await Wrapper.Select(connection, new() { { KEY_ID, ("=", newId) } }, null, null, cancellationToken);
      await foreach (R resource in a)
      {
        Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, $"#{resource.ID} inserted to the database.");
        return resource;
      }

      throw new InvalidOperationException("Failed to get new inserted resource.");
    }

    protected async Task<bool> DbUpdate(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> where, Dictionary<string, object?> newData, CancellationToken cancellationToken)
    {
      int output = 0;

      await using var stream = await Wrapper.Select(connection, where, null, null, cancellationToken);
      await foreach (R resource in stream)
      {
        if (!await Wrapper.Update(connection, new() { { KEY_ID, ("=", resource.ID) } }, newData, cancellationToken))
        {
          continue;
        }

        Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, $"#{resource.ID} updated on the database.");
        output++;
      }

      return output != 0;
    }

    protected virtual async Task<bool> DbDelete(SQLiteConnection connection, Dictionary<string, (string condition, object? value)> data, CancellationToken cancellationToken)
    {
      int output = 0;

      await using var stream = await Wrapper.Select(connection, data, null, null, cancellationToken);
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

    public Task<R?> GetByID(SQLiteConnection connection, string idHex, CancellationToken cancellationToken) => GetByID(connection, idHex, null, cancellationToken);
    public Task<R?> GetByID(SQLiteConnection connection, string idHex, int? offset, CancellationToken cancellationToken) => GetByID(connection, Buffer.From(idHex, StringEncoding.Hex).ToUInt64(), offset, cancellationToken);

    public Task<R?> GetByID(SQLiteConnection connection, ulong id, CancellationToken cancellationToken) => GetByID(connection, id, null, cancellationToken);
    public Task<R?> GetByID(SQLiteConnection connection, ulong id, int? offset, CancellationToken cancellationToken) => Wrapper.SelectOne(connection, new() { { KEY_ID, ("=", id) } }, offset, cancellationToken);

    public async override Task Init(CancellationToken cancellationToken)
    {
      int? version = null;

      await Database.RunTransaction(async (connection, cancellationToken) =>
      {
        await InitVersioningTable(connection, cancellationToken);
        version = await GetResourceVersion(connection, Name, cancellationToken);
      }, cancellationToken);

      await Database.RunTransaction(async (connection, cancellationToken) =>
      {
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
      }, cancellationToken);

      await Database.RunTransaction((connection, cancellationToken) => SetResourceVersion(connection, Name, Version, cancellationToken), cancellationToken);
    }

    private D CreateData(SQLiteDataReader reader) => CreateData(reader,
      (ulong)(long)reader[KEY_ID],
      (ulong)(long)reader[KEY_CREATE_TIME],
      (ulong)(long)reader[KEY_UPDATE_TIME]
    );

    protected abstract D CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime);
    protected abstract Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken);
    protected abstract Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken);

    public virtual async Task<bool> Delete(SQLiteConnection connection, R resource, CancellationToken cancellationToken)
    {
      if (resource.IsDeleted)
      {
        return false;
      }

      foreach (ResourceDeleteHandle handle in ResourceDeleteHandles)
      {
        await handle(connection, resource, cancellationToken);
      }

      resource.IsDeleted = true;
      if (!await Wrapper.Delete(connection, new() { { KEY_ID, ("=", resource.ID) } }, cancellationToken))
      {
        return false;
      }

      Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, $"#{resource.ID} deleted from the database.");
      return true;
    }
  }

  protected Resource(M manager, D data) : base(manager, data)
  {
    Logger = new(IDHex);
    IsDeleted = false;

    manager.Logger.Subscribe(Logger);
    Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Resource Constructed");
  }

  ~Resource()
  {
    Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Resource Destroyed");
    Manager.Logger.Unsubscribe(Logger);
  }

  public readonly EnderBytesLogger Logger;

  protected override void UpdateData(D data)
  {
    Logger.Log(EnderBytesLogger.LOGLEVEL_VERBOSE, "Resource Updated");
  }

  public bool IsDeleted { get; private set; }
  public string IDHex => Buffer.From(ID).ToHexString();
}

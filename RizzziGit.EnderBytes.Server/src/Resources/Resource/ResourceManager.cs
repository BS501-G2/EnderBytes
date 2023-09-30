namespace RizzziGit.EnderBytes.Resources;

using System.Data.SQLite;
using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    protected ResourceManager(MainResourceManager main, int version, string name) : base(main, version, name)
    {
      Main = main;
      Wrapper = new((M)this);
      Logger = new(name);

      Main.Logger.Subscribe(Logger);
    }

    ~ResourceManager()
    {
      Main.Logger.Unsubscribe(Logger);
    }

    public new readonly MainResourceManager Main;
    public readonly DatabaseWrapper Wrapper;
    public readonly EnderBytesLogger Logger;

    public Database Database => Main.RequireDatabase();

    protected ulong GenerateTimestamp() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    protected async Task<R> DbInsert(SQLiteConnection connection, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
      ulong timestamp = GenerateTimestamp();

      data.Add(KEY_CREATE_TIME, timestamp);
      data.Add(KEY_UPDATE_TIME, timestamp);

      ulong newId = await Wrapper.Insert(connection, data, cancellationToken);

      await using var a = (ResourceStream)await Wrapper.Select(connection, new() { { KEY_ID, ("=", newId) } }, null, null, cancellationToken);
      await foreach (R resource in a)
      {
        return resource;
      }

      throw new InvalidOperationException("Failed to get new inserted resource.");
    }
  }
}

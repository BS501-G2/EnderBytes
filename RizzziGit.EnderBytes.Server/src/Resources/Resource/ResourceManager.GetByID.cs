using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Buffer;
using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    public Task<R?> GetByID(string idHex, CancellationToken cancellationToken) => GetByID(Buffer.From(idHex, StringEncoding.Hex).ToUInt64(), cancellationToken);
    public Task<R?> GetByID(SQLiteConnection connection, string idHex, CancellationToken cancellationToken) => GetByID(connection, Buffer.From(idHex, StringEncoding.Hex).ToUInt64(), cancellationToken);

    public Task<R?> GetByID(ulong id, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => GetByID(connection, id, cancellationToken), cancellationToken);
    public async Task<R?> GetByID(SQLiteConnection connection, ulong id, CancellationToken cancellationToken)
    {
      await using var stream = (ResourceStream)await Wrapper.Select(connection, new() { { KEY_ID, ("=", id) } }, 0, 1, cancellationToken);
      await foreach(R resource in stream)
      {
        return resource;
      }

      return null;
    }
  }
}

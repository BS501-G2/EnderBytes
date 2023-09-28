namespace RizzziGit.EnderBytes.Resources;

using System.Data.SQLite;
using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    public Task<R?> GetByID(ulong id, CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => GetByID(connection, id, cancellationToken), cancellationToken);
    public async Task<R?> GetByID(SQLiteConnection connection, ulong id, CancellationToken cancellationToken)
    {
      using SQLiteDataReader reader = await connection.ExecuteReaderAsync($"select * from {Name} where {Resource<M, D, R>.KEY_ID} = {{0}} limit 1;", cancellationToken, []);
      if (await reader.ReadAsync(cancellationToken))
      {
        return AsResource(OnCreateData(reader));
      }

      return null;
    }
  }
}

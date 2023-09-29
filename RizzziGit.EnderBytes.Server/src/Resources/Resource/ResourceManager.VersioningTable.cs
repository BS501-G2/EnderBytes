namespace RizzziGit.EnderBytes.Resources;

using System.Data.SQLite;
using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    private const string RV_TABLE = "__TableVersions";
    private const string RV_COLUMN_NAME = "Name";
    private const string RV_COLUMN_VERSION = "Version";

    private static Task InitVersioningTable(SQLiteConnection connection, CancellationToken cancellationToken) => connection.ExecuteNonQueryAsync(@$"create table if not exists {RV_TABLE}({RV_COLUMN_NAME} varchar(128) primary key, {RV_COLUMN_VERSION} integer not null);", cancellationToken);
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
  }
}

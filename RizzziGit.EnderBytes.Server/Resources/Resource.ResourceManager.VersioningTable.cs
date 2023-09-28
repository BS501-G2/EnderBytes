namespace RizzziGit.EnderBytes.Resources;

using System.Data.SQLite;
using Database;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceManager : Shared.Resources.Resource<M, D, R>.ResourceManager, Shared.Resources.IResourceManager
  {
    private const string RV_TABLE = "__TableVersinos";
    private const string RV_COLUMN_NAME = "Name";
    private const string RV_COLUMN_VERSION = "VERSION";

    private Task InitVersioningTable(CancellationToken cancellationToken) => Database.RunTransaction((connection, cancellationToken) => connection.ExecuteNonQueryAsync(@$"create table if not exists {RV_TABLE}({RV_COLUMN_NAME} varchar(128) primary key not null, {RV_COLUMN_VERSION} integer not null);", cancellationToken), cancellationToken);
    private Task<int?> GetResourceVersion(CancellationToken cancellationToken) => Database.RunTransaction<int?>(async (connection, cancellationToken) =>
    {
      SQLiteDataReader reader = await connection.ExecuteReaderAsync(@$"select * from {RV_TABLE} where {RV_COLUMN_NAME} = {{0}} limit 1;", cancellationToken, [Name]);

      return reader.Read() ? (int)reader[RV_COLUMN_VERSION] : null;
    }, cancellationToken);
  }
}

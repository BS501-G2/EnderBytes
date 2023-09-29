using System.Data.SQLite;
using RizzziGit.EnderBytes.Database;

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public const string KEY_ID = "ID";
  public const string KEY_CREATE_TIME = "CreateTime";
  public const string KEY_UPDATE_TIME = "UpdateTime";

  public abstract partial class ResourceManager
  {
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
  }
}

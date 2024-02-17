using System.Data.Common;

namespace RizzziGit.EnderBytes.DatabaseWrappers;

public abstract class Database : IDisposable
{
  public Database(DbConnectionStringBuilder connectionStringBuilder)
  {
    Connection = InternalCreateConnection(connectionStringBuilder.ToString());
  }

  public readonly DbConnection Connection;

  protected abstract DbConnection InternalCreateConnection(string connectionString);
  protected abstract DbParameter InternalCreateParameter(string name, object? value);

  public DbParameter CreateParameter(string name, object? value) => InternalCreateParameter(ToParameterName(name), value);
  public abstract string ToParameterName(string name);

  public void Dispose()
  {
    Connection.Dispose();
    GC.SuppressFinalize(this);
  }
}

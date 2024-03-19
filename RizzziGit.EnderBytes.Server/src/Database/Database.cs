using System.Data.Common;

namespace RizzziGit.EnderBytes.DatabaseWrappers;

public abstract class Database(DbConnectionStringBuilder connectionStringBuilder) : IDisposable
{
  public delegate void DatabaseConnectionHandler(DbConnection connection, CancellationToken cancellationToken = default);

  private readonly string ConnectionString = connectionStringBuilder.ToString();
  private bool Disposed = false;

  protected abstract DbConnection InternalCreateConnection(string connectionString);
  protected abstract DbParameter InternalCreateParameter(string name, object? value);

  public DbParameter CreateParameter(string name, object? value) => InternalCreateParameter(ToParameterName(name), value);
  public abstract string ToParameterName(string name);

  public async Task Run(DatabaseConnectionHandler handler, CancellationToken cancellationToken = default)
  {
    using DbConnection connection = InternalCreateConnection(ConnectionString);
    try
    {
      await connection.OpenAsync(cancellationToken);
      handler(connection, cancellationToken);
    }
    finally
    {
      await connection.CloseAsync();
    }
  }

  public void Dispose()
  {
    lock (this)
    {
      if (Disposed)
      {
        return;
      }

      Disposed = true;

      GC.SuppressFinalize(this);
    }
  }
}

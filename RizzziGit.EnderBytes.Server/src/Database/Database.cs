using System.Data.Common;

namespace RizzziGit.EnderBytes.DatabaseWrappers;

using Commons.Logging;

public abstract class Database(DbConnectionStringBuilder connectionStringBuilder) : IDisposable
{
    public delegate Task DatabaseConnectionHandler(DbConnection connection);

    private readonly string ConnectionString = connectionStringBuilder.ToString();
    private bool Disposed = false;

    protected abstract DbConnection InternalCreateConnection(string connectionString);
    protected abstract DbParameter InternalCreateParameter(string name, object? value);

    public DbParameter CreateParameter(string name, object? value) =>
        InternalCreateParameter(ToParameterName(name), value);

    public abstract string ToParameterName(string name);

    public async Task Run(Logger logger, long transactionId, DatabaseConnectionHandler handler)
    {
        while (true)
        {
            try
            {
                await using DbConnection connection = InternalCreateConnection(ConnectionString);

                try
                {
                    await connection.OpenAsync();

                    {
                        using DbCommand command = connection.CreateCommand();
                        command.CommandText = "PRAGMA synchronous=OFF; PRAGMA journal_mode=MEMORY;";
                        await command.ExecuteNonQueryAsync();
                    }

                    await handler(connection);
                }
                finally
                {
                    await connection.CloseAsync();
                }

                break;
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("database table is locked"))
                {
                    logger.Log(
                        LogLevel.Info,
                        $"[Transaction #{transactionId}] Deadlock detected. Restarting transaction..."
                    );
                    continue;
                }

                throw;
            }
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

using System.Runtime.CompilerServices;
using System.Data.Common;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;
using Commons.Logging;

using Utilities;
using Core;
using System.Runtime.ExceptionServices;
using System.Diagnostics.CodeAnalysis;

public sealed partial class ResourceService
{
    public delegate Task TransactionHandler(Transaction transaction);
    public delegate Task<T> TransactionHandler<T>(Transaction transaction);
    public delegate IAsyncEnumerable<T> TransactionEnumeratorHandler<T>(Transaction transaction);

    private long NextTransactionId;
    public sealed record Transaction(DbConnection Connection, long Id, ResourceService ResourceService)
    {
        private static string ContextKey<T>(string key) => $"{key}_{typeof(T).FullName}";

        public Server Server => ResourceService.Server;

        private readonly WeakDictionary<string, object> ContextValues = [];

        public void SetOrUpdateContextValue<T>(string key, T value) where T : class
        {
            string contextKey = ContextKey<T>(key);

            ContextValues.Remove(contextKey);
            ContextValues.Add(contextKey, value);
        }

        public bool TryGetContextValue<T>(string key, [NotNullWhen(true)] out T? value) where T : class
        {
            ContextValues.TryGetValue(ContextKey<T>(key), out object? target);
            value = target as T;
            return value != null;
        }

        public T GetContextValue<T>(string key) where T : class
        {
            if (!TryGetContextValue<T>(key, out T? value))
            {
                throw new KeyNotFoundException($"Context value '{key}' not found.");
            }

            return value;
        }

        public T GetManager<T>() where T : ResourceManager => ResourceService.GetManager<T>();
    }

    public async IAsyncEnumerable<T> EnumeratedTransact<T>(TransactionEnumeratorHandler<T> handler)
    {
        WaitQueue<StrongBox<T>?> waitQueue = new(0);
        ExceptionDispatchInfo? exceptionDispatchInfo = null;

        _ = Transact(async (transaction) =>
        {
            try
            {
                await foreach (T item in handler(transaction))
                {
                    await waitQueue.Enqueue(new(item));
                }
            }
            catch (Exception exception)
            {
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
            }

            await waitQueue.Enqueue(null);
        });

        await foreach (StrongBox<T>? item in waitQueue)
        {
            if (item == null)
            {
                break;
            }

            yield return item.Value!;
        }

        if (exceptionDispatchInfo != null)
        {
            exceptionDispatchInfo.Throw();
        }
    }

    public async Task<T> Transact<T>(TransactionHandler<T> handler)
    {
        TaskCompletionSource<T> source = new();

        try
        {
            await Transact(async (transaction) => source.SetResult(await handler(transaction)));
        }
        catch (Exception exception)
        {
            source.SetException(exception);
        }

        return await source.Task;
    }

    public async Task Transact(TransactionHandler handler)
    {
        long transactionId = NextTransactionId++;

        GetCancellationToken().ThrowIfCancellationRequested();

        await Database!.Run(Logger, transactionId, async (connection) =>
        {
            await using DbTransaction dbTransaction = await connection.BeginTransactionAsync();

            Transaction transaction = new(connection, transactionId, this);
            Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction begin.");

            try
            {
                await handler(transaction);
                Logger.Log(LogLevel.Debug, $"[Transaction #{transaction.Id}] Transaction commit.");

                await dbTransaction.CommitAsync();
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.Warn, $"[Transaction #{transaction.Id}] Transaction rollback due to exception: [{exception.GetType().Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}");

                await dbTransaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }
}

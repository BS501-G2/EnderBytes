using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Utilities;

using Framework.Collections;

public static class IMongoClientExtensions
{
  private static readonly WeakDictionary<IClientSessionHandle, List<OnFailure>> Callbacks = [];

  public delegate void OnFailure();

  public delegate Task AsyncCallback(CancellationToken cancellationToken);
  public delegate Task<T> AsyncCallback<T>(CancellationToken cancellationToken);
  public delegate void Callback();
  public delegate T Callback<T>();

  public static Task RunTransaction(this IMongoClient client, Callback callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default) => RunTransaction(client, (_) => { callback(); return Task.CompletedTask; }, sessionOptions, transactionOptions, cancellationToken);
  public static Task<T> RunTransaction<T>(this IMongoClient client, Callback<T> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default) => RunTransaction(client, (_) => Task.FromResult(callback()), sessionOptions, transactionOptions, cancellationToken);

  public static async Task RunTransaction(this IMongoClient client, AsyncCallback callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
  {
    using var session = await client.StartSessionAsync(sessionOptions, cancellationToken);
    session.StartTransaction(transactionOptions);

    try
    {
      await callback(cancellationToken);
    }
    catch
    {
      await session.AbortTransactionAsync(CancellationToken.None);
      throw;
    }
    finally
    {
      await session.CommitTransactionAsync(cancellationToken);
    }
  }

  public static async Task<T> RunTransaction<T>(this IMongoClient client, AsyncCallback<T> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<T> source = new();

    try
    {
      await client.RunTransaction(async (cancellationToken) => source.SetResult(await callback(cancellationToken)), sessionOptions, transactionOptions, cancellationToken);
    }
    catch (Exception exception)
    {
      source.SetException(exception);
    }

    return await source.Task;
  }
}

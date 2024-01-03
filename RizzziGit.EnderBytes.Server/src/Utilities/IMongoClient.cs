using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Utilities;

using System.Runtime.CompilerServices;

public static class IMongoClientExtensions
{
  public delegate void OnFailure();

  public delegate void Callback(CancellationToken cancellationToken);
  public delegate T Callback<T>(CancellationToken cancellationToken);

  public static void RunTransaction(this IMongoClient client, Action callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default) => client.RunTransaction((_) => callback(), sessionOptions, transactionOptions, cancellationToken);
  public static void RunTransaction(this IMongoClient client, Callback callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    lock (client)
    {
      using IClientSessionHandle handle = client.StartSession(sessionOptions, cancellationToken);
      handle.StartTransaction(transactionOptions);

      try
      {
        callback(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        handle.CommitTransaction(cancellationToken);
      }
      catch
      {
        handle.AbortTransaction(cancellationToken);
        throw;
      }
    }
  }

  public static T RunTransaction<T>(this IMongoClient client, Func<T> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default) => client.RunTransaction((_) => callback(), sessionOptions, transactionOptions, cancellationToken);
  public static T RunTransaction<T>(this IMongoClient client, Callback<T> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
  {
    StrongBox<T>? result = null;
    client.RunTransaction((cancellationToken) => { result = new(callback(cancellationToken)); }, sessionOptions, transactionOptions, cancellationToken);

    return result!.Value!;
  }
}

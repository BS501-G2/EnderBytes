using MongoDB.Driver;
using RizzziGit.EnderBytes.Records;

namespace RizzziGit.EnderBytes.Utilities;

public static class IMongoCollection
{
  public delegate Task HookAsync<T>(ChangeStreamDocument<T> t);
  public delegate void Hook<T>(ChangeStreamDocument<T> t);

  public static long GetNewId<T>(this IMongoCollection<T> mongoCollection)
    where T : Record
  {
    long id;

    do
    {
      id = Random.Shared.NextInt64();
    }
    while ((from record in mongoCollection.AsQueryable() where record.Id == id select record).First() != null);

    return id;
  }

  public static void BeginWatching<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, CancellationToken cancellationToken) => mongoCollection.Watch((change) => { hook(change); return Task.CompletedTask; }, null, cancellationToken);
  public static void BeginWatching<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => mongoCollection.Watch((change) => { hook(change); return Task.CompletedTask; }, options, cancellationToken);

  public static void Watch<T>(this IMongoCollection<T> mongoCollection, HookAsync<T> hook, CancellationToken cancellationToken) => mongoCollection.Watch(hook, null, cancellationToken);
  public static async void Watch<T>(this IMongoCollection<T> mongoCollection, HookAsync<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
  {
    await foreach (ChangeStreamDocument<T> document in mongoCollection.Watch(options, cancellationToken).Wrap(cancellationToken))
    {
      await hook(document);
    }
  }
}

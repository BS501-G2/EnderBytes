using System.Linq.Expressions;
using MongoDB.Driver;
using RizzziGit.Framework.Collections;

namespace RizzziGit.EnderBytes.Utilities;

public static class IMongoCollectionExtensions
{
  public delegate void Hook<T>(ChangeStreamDocument<T> t);

  public static Task WatchAsync<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, CancellationToken cancellationToken) => mongoCollection.WatchAsync(hook, null, cancellationToken);
  public static async Task WatchAsync<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
  {
    await foreach (ChangeStreamDocument<T> change in mongoCollection.Watch(options, cancellationToken).ToAsyncEnumerable(cancellationToken))
    {
      hook(change);
    }
  }

  public static T? FindOne<T> (this IMongoCollection<T> mongoCollection, Expression<Func<T, bool>> filter, FindOptions? options = null, CancellationToken cancellationToken = default) => mongoCollection.Find(filter, options).FirstOrDefault(cancellationToken);

  private sealed record CollectionNextId(string Collection, long NextId);
  public static long GenerateNewId<T>(this IMongoCollection<T> collection, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IMongoCollection<CollectionNextId> versionCollection = collection.Database.GetCollection<CollectionNextId>("_Ids");
    string collectionName = collection.CollectionNamespace.CollectionName;

    long version = versionCollection.FindOneAndDelete((version) => version.Collection == collectionName, cancellationToken: cancellationToken)?.NextId ?? 0;
    versionCollection.InsertOne(new(collectionName, version + 1), cancellationToken: cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();
    return version;
  }
}

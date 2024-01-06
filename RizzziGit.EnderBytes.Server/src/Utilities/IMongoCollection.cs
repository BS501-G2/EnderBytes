using System.Linq.Expressions;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Utilities;

public static class IMongoCollection
{
  public delegate void Hook<T>(ChangeStreamDocument<T> t);

  public static void Watch<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, CancellationToken cancellationToken) => mongoCollection.Watch(hook, null, cancellationToken);
  public static void Watch<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
  {
    foreach (ChangeStreamDocument<T> change in mongoCollection.Watch(options, cancellationToken).ToEnumerable(cancellationToken))
    {
      hook(change);
    }
  }

  public static T? FindOne<T> (this IMongoCollection<T> mongoCollection, Expression<Func<T, bool>> filter, FindOptions? options = null, CancellationToken cancellationToken = default) => mongoCollection.Find(filter, options).FirstOrDefault(cancellationToken);

  private sealed record CollectionNextId(string Collection, long NextId);
  public static (long Id, long CreateTime, long UpdateTime) GenerateNewId<T>(this IMongoCollection<T> collection, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IMongoCollection<CollectionNextId> versionCollection = collection.Database.GetCollection<CollectionNextId>();
    string collectionName = collection.CollectionNamespace.CollectionName;

    long version = versionCollection.FindOneAndDelete((version) => version.Collection == collectionName, cancellationToken: cancellationToken)?.NextId ?? 0;
    versionCollection.InsertOne(new(collectionName, version + 1), cancellationToken: cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();
    long createTime, updateTime = createTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    return (version, createTime, updateTime);
  }
}

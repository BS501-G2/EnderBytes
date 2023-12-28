using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Utilities;

using Records;

public static class IMongoCollection
{
  public delegate Task HookAsync<T>(ChangeStreamDocument<T> t, CancellationToken cancellationToken);
  public delegate void Hook<T>(ChangeStreamDocument<T> t);

  public static void Watch<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, CancellationToken cancellationToken) => mongoCollection.Watch((change, _) => { hook(change); return Task.CompletedTask; }, null, cancellationToken);
  public static void Watch<T>(this IMongoCollection<T> mongoCollection, Hook<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default) => mongoCollection.Watch((change, _) => { hook(change); return Task.CompletedTask; }, options, cancellationToken);

  public static void Watch<T>(this IMongoCollection<T> mongoCollection, HookAsync<T> hook, CancellationToken cancellationToken) => mongoCollection.Watch(hook, null, cancellationToken);
  public static async void Watch<T>(this IMongoCollection<T> mongoCollection, HookAsync<T> hook, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
  {
    try
    {
      await foreach (ChangeStreamDocument<T> document in mongoCollection.Watch(options, cancellationToken).ToAsyncEnumerable(cancellationToken))
      {
        await hook(document, cancellationToken);
      }
    }
    catch (OperationCanceledException) { }
  }

  private static string GenerateQueryString<T>(IQueryable<T> queryable) => new JObject { { nameof(Record.Id), new JObject { { "$in", new JArray([.. queryable]) } } } }.ToString();

  public static T? FindOne<T> (this IMongoCollection<T> mongoCollection, Expression<Func<T, bool>> filter, FindOptions? options = null, CancellationToken cancellationToken = default) => mongoCollection.Find(filter, options).FirstOrDefault(cancellationToken);
  public static async Task<T?> FindOneAsync<T> (this IMongoCollection<T> mongoCollection, Expression<Func<T, bool>> filter, FindOptions<T, T>? options = null, CancellationToken cancellationToken = default) => await (await mongoCollection.FindAsync(filter, options, cancellationToken)).FirstOrDefaultAsync(cancellationToken);

  public static DeleteResult DeleteMany<T>(this IMongoCollection<T> mongoCollection, IQueryable<T> queryable) where T : Record => mongoCollection.DeleteMany(GenerateQueryString(queryable));
  public static Task<DeleteResult> DeleteManyAsync<T>(this IMongoCollection<T> mongoCollection, IQueryable<T> queryable) where T : Record => mongoCollection.DeleteManyAsync(GenerateQueryString(queryable));

  public static DeleteResult DeleteMany<T>(this IMongoCollection<T> mongoCollection, IMongoQueryable<T> queryable) where T : Record => mongoCollection.DeleteMany(GenerateQueryString(queryable));
  public static Task<DeleteResult> DeleteManyAsync<T>(this IMongoCollection<T> mongoCollection, IMongoQueryable<T> queryable) where T : Record => mongoCollection.DeleteManyAsync(GenerateQueryString(queryable));
}

using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Utilities;

using Records;

public static class IMongoDatabaseExtensions
{
  public static IMongoCollection<R> GetCollection<R>(this IMongoDatabase mongoDatabase, MongoCollectionSettings? settings = null) where R : Record => mongoDatabase.GetCollection<R>(nameof(R), settings);
}

using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Utilities;

public static class IMongoDatabaseExtensions
{
  public static IMongoCollection<R> GetCollection<R>(this IMongoDatabase mongoDatabase, MongoCollectionSettings? settings = null) => mongoDatabase.GetCollection<R>(nameof(R), settings);
}

using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Resources;

using Database;
using Extensions;
using Collections;

public sealed class BlobFileKeyResource(BlobFileKeyResource.ResourceManager manager, BlobFileKeyResource.ResourceData data) : Resource<BlobFileKeyResource.ResourceManager, BlobFileKeyResource.ResourceData, BlobFileKeyResource>(manager, data)
{
  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobFileKeyResource>.ResourceManager
  {
    private const string NAME = "BlobFileKey";
    private const int VERSION = 1;

    private const string KEY_FILE_ID = "FileId";
    private const string KEY_KEY_ID = "KeyId";
    private const string KEY_PAYLOAD = "Payload";

    public ResourceManager(MainResourceManager main, Database database) : base(main, database, NAME, VERSION)
    {
      RNG = RandomNumberGenerator.Create();

      main.BlobFiles.OnResourceDelete((transaction, resource, cancellationToken) => DbDelete(transaction, new()
      {
        { KEY_FILE_ID, ("=", resource.Id, null) }
      }, cancellationToken));
    }

    private readonly RandomNumberGenerator RNG;

    protected override BlobFileKeyResource CreateResource(ResourceData data) => new(this, data);
    protected override ResourceData CreateData(SqliteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_FILE_ID],
      (long)reader[KEY_KEY_ID],
      (byte[])reader[KEY_PAYLOAD]
    );

    protected override void OnInit(DatabaseTransaction transaction) => OnInit(0, transaction);
    protected override void OnInit(int oldVersion, DatabaseTransaction transaction)
    {
      if (oldVersion < 1)
      {
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_FILE_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_KEY_ID} integer not null;");
        transaction.ExecuteNonQuery($"alter table {NAME} add column {KEY_PAYLOAD} blob not null;");
      }
    }

    public BlobFileKeyResource Create(
      DatabaseTransaction transaction,
      BlobFileResource file,
      KeyResource key
    )
    {
      if (file.Type != BlobFileResource.TYPE_FILE)
      {
        throw new ArgumentException("Invalid file resource type.", nameof(file));
      }

      foreach (var _ in DbStream(transaction, new()
      {
        { KEY_FILE_ID, ("=", file.Id, null) }
      }, (1, null), null))
      {
        throw new InvalidOperationException("Cannot generate new keys for file.");
      }

      foreach (KeyDataResource keyData in Main.KeyData.List(transaction, key, (1, null), null))
      {
        return DbInsert(transaction, new()
        {
          { KEY_FILE_ID, file.Id },
          { KEY_KEY_ID, key.Id },
          { KEY_PAYLOAD, keyData.Encrypt(RNG.GetBytes(32)) }
        });
      }

      throw new InvalidOperationException("No key data available yet.");
    }
  }


  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long FileId,
    long KeyId,
    byte[] Payload
  ) : Resource<ResourceManager, ResourceData, BlobFileKeyResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long FileId => Data.FileId;
  public long KeyId => Data.KeyId;
  public byte[] Payload => Data.Payload;
}

using System.Data.SQLite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class BlobStorageKeyResource(BlobStorageKeyResource.ResourceManager manager, BlobStorageKeyResource.ResourceData data) : Resource<BlobStorageKeyResource.ResourceManager, BlobStorageKeyResource.ResourceData, BlobStorageKeyResource>(manager, data)
{
  public const string NAME = "VSFKey";
  public const int VERSION = 1;

  private const string KEY_OBSOLESCENCE_TIME = "ObsolescenceTime";
  private const string KEY_INDEX = "KeyIndex";
  private const string KEY_PASSWORD_ID = "PasswordID";
  private const string KEY_IV = "IV";
  private const string KEY_PAYLOAD = "Payload";

  private const string INDEX_UNIQUENESS = $"Index_{NAME}_{KEY_PASSWORD_ID}_{KEY_INDEX}";

  public new sealed class ResourceData(
    ulong id,
    long createTime,
    long updateTime,
    long obsolescenceTime,
    uint index,
    ulong passwordId,
    byte[] iv,
    byte[] payload
  ) : Resource<ResourceManager, ResourceData, BlobStorageKeyResource>.ResourceData(id, createTime, updateTime)
  {
    public long ObsolescenceTime = obsolescenceTime;
    public uint Index = index;
    public ulong PasswordID = passwordId;
    public byte[] IV = iv;
    public byte[] Payload = payload;
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, BlobStorageKeyResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main) : base(main, VERSION, NAME)
    {
      Generator = RandomNumberGenerator.Create();

      main.UserAuthentications.ResourceDeleteListeners.Add(async (connection, resource, cancellationToken) =>
      {
        await DbDelete(connection, new()
        {
          { KEY_PASSWORD_ID, ("=", resource.ID, null) }
        }, cancellationToken);
      });
    }

    private readonly RandomNumberGenerator Generator;

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      (long)reader[KEY_OBSOLESCENCE_TIME],
      (uint)(long)reader[KEY_INDEX],
      (ulong)(long)reader[KEY_PASSWORD_ID],
      (byte[])reader[KEY_IV],
      (byte[])reader[KEY_PAYLOAD]
    );

    protected override BlobStorageKeyResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_OBSOLESCENCE_TIME} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_INDEX} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_PASSWORD_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_IV} blob not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_PAYLOAD} blob not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_UNIQUENESS} on {NAME}({KEY_PASSWORD_ID},{KEY_INDEX});", cancellationToken);
      }
    }

    public Task<ResourceStream> Stream(SQLiteConnection connection, UserAuthenticationResource userAuthentication, CancellationToken cancellationToken) => DbSelect(connection, new()
    {
      { KEY_PASSWORD_ID, ("=", userAuthentication.ID, null) }
    }, null, null, cancellationToken);

    public async Task<BlobStorageKeyResource> Create(SQLiteConnection connection, UserAuthenticationResource userAuthentication, byte[] passwordHash, CancellationToken cancellationToken)
    {
      if (userAuthentication.Type != UserAuthenticationResource.TYPE_PASSWORD_HASH_IV)
      {
        throw new ArgumentException("Invalid user authentication type.", nameof(userAuthentication));
      }

      byte[] iv = new byte[16];
      byte[] key = new byte[32];

      Generator.GetBytes(iv);
      Generator.GetBytes(key);

      byte[] payload = Aes.Create().CreateEncryptor(passwordHash, iv).TransformFinalBlock(key, 0, key.Length);
      uint index;

      do
      {
        index = (uint)Random.Shared.Next();
      }
      while (await DbSelectOne(connection, new() { { KEY_INDEX, ("=", index, null) } }, null, null, cancellationToken) != null);
      return await DbInsert(connection, new()
      {
        { KEY_OBSOLESCENCE_TIME, GenerateTimestamp() + Main.Server.Config.ObsolescenceTimeSpan },
        { KEY_PASSWORD_ID, userAuthentication.ID },
        { KEY_INDEX, index },
        { KEY_IV, iv },
        { KEY_PAYLOAD, payload }
      }, cancellationToken);
    }

    public async Task<List<BlobStorageKeyResource>> Clone(
      SQLiteConnection connection,
      UserAuthenticationResource oldUserAuthentication,
      byte[] oldPasswordHash,
      UserAuthenticationResource newUserAuthentication,
      byte[] newPasswordHash,
      CancellationToken cancellationToken
    )
    {
      byte[] iv = new byte[16];
      Generator.GetBytes(iv);

      await using var stream = await DbSelect(connection, new() { { KEY_PASSWORD_ID, ("=", oldUserAuthentication.ID, null) } }, null, null, cancellationToken);
      List<BlobStorageKeyResource> list = [];
      await foreach (BlobStorageKeyResource resource in stream)
      {
        byte[] key = Aes.Create().CreateDecryptor(oldPasswordHash, resource.IV).TransformFinalBlock(resource.Payload, 0, resource.Payload.Length);
        byte[] payload = Aes.Create().CreateEncryptor(newPasswordHash, iv).TransformFinalBlock(key, 0, key.Length);

        list.Add(await DbInsert(connection, new()
        {
          { KEY_OBSOLESCENCE_TIME, GenerateTimestamp() + Main.Server.Config.ObsolescenceTimeSpan },
          { KEY_PASSWORD_ID, newUserAuthentication.ID },
          { KEY_INDEX, resource.Index },
          { KEY_IV, iv },
          { KEY_PAYLOAD, payload }
        }, cancellationToken));
      }

      return list;
    }

    public byte[] GetKey(BlobStorageKeyResource blobStorageKey, byte[] passwordHash) => Aes.Create().CreateDecryptor(passwordHash, blobStorageKey.IV).TransformFinalBlock(blobStorageKey.Payload, 0, blobStorageKey.Payload.Length);
  }

  public long ObsolescenceTime => Data.ObsolescenceTime;
  public uint Index => Data.Index;
  public ulong PasswordID => Data.PasswordID;
  public byte[] IV => Data.IV;
  public byte[] Payload => Data.Payload;
}

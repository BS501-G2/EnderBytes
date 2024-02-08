using System.Data.SQLite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileNodeResource(FileNodeResource.ResourceManager manager, FileNodeResource.ResourceData data) : Resource<FileNodeResource.ResourceManager, FileNodeResource.ResourceData, FileNodeResource>(manager, data)
{
  public enum FileNodeType : byte { File, Folder, SymbolicLink }

  private const string NAME = "File";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceManager
  {
    private const string COLUMN_HUB_ID = "HubId";
    private const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    private const string COLUMN_TYPE = "Type";
    private const string COLUMN_NAME = "Name";
    private const string COLUMN_ENCRYPTED_AES_KEY = "EncryptedAesKey";
    private const string COLUMN_ENCRYPTED_AES_IV = "EncryptedAesIv";

    private const string UNIQUE_NAME = $"Unique_{NAME}_{COLUMN_NAME}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      service.FileHubs.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_HUB_ID, "=", resource.Id));
    }

    protected override FileNodeResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => throw new Exception();

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_HUB_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_FILE_ID} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null collate nocase;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_AES_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_ENCRYPTED_AES_IV} blob not null;");

        SqlNonQuery(transaction, $"create unique index {UNIQUE_NAME} on {NAME}({COLUMN_HUB_ID},{COLUMN_PARENT_FILE_ID},{COLUMN_NAME});");
      }
    }

    public FileNodeResource Create(ResourceService.Transaction transaction, UserAuthenticationResource.Token token, FileHubResource hub, FileNodeResource? parentFolder, FileNodeType type, string name)
    {
      hub.ThrowIfInvalid();

      if (parentFolder != null)
      {
        parentFolder.ThrowIfInvalid();

        if (parentFolder.Type != FileNodeType.Folder)
        {
          throw new ArgumentException("Invalid parent folder node.", nameof(parentFolder));
        }
      }

      if (Exists(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_HUB_ID, "=", hub.Id),
        new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", parentFolder?.Id),
        new WhereClause.CompareColumn(COLUMN_NAME, "=", name)
      )))
      {
        throw new ArgumentException("File exists.", nameof(name));
      }

      KeyService.AesPair hubAesPair = parentFolder == null
        ? hub.DecryptAesPair(token)
        : DecryptAesPair(transaction, parentFolder, token);

      return Insert(transaction, new(
        (COLUMN_HUB_ID, hub.Id),
        (COLUMN_PARENT_FILE_ID, parentFolder?.Id),
        (COLUMN_TYPE, (byte)type),
        (COLUMN_NAME, name),

        (COLUMN_ENCRYPTED_AES_KEY, hubAesPair.Encrypt(RandomNumberGenerator.GetBytes(32))),
        (COLUMN_ENCRYPTED_AES_IV, hubAesPair.Encrypt(RandomNumberGenerator.GetBytes(16)))
      ));
    }

    public KeyService.AesPair DecryptAesPair(ResourceService.Transaction transaction, FileNodeResource node, UserAuthenticationResource.Token? token)
    {
      token?.ThrowIfInvalid();

      FileHubResource hub = Service.FileHubs.GetById(transaction, node.HubId)!;

      return decryptAesPair(node);

      KeyService.AesPair decryptAesPair(FileNodeResource node)
      {
        node.ThrowIfInvalid();

        if (hub.Id != node.HubId)
        {
          throw new ArgumentException("File is not located the hub.", nameof(node));
        }

        if (hub.OwnerUserId == token?.UserId)
        {
          KeyService.AesPair aesPair = node.ParentFileId == null
            ? hub.DecryptAesPair(token)
            : decryptAesPair(GetById(transaction, (long)node.ParentFileId)!);

          return new(aesPair.Decrypt(node.EncryptedAesKey), aesPair.Decrypt(node.EncryptedAesIv));
        }

        return (
          Service.FileAccesses.GetByToken(transaction, token)
            ?? throw new ArgumentException("No access granted for token.", nameof(token))
        ).DecryptAesPair(token);
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long HubId,
    FileNodeType Type,
    long? ParentFileId,
    string Name,

    byte[] EncryptedAesKey,
    byte[] EncryptedAesIv
  ) : Resource<ResourceManager, ResourceData, FileNodeResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long HubId => Data.HubId;
  public FileNodeType Type => Data.Type;
  public long? ParentFileId => Data.ParentFileId;
  public string Name => Data.Name;

  private byte[] EncryptedAesKey => Data.EncryptedAesKey;
  private byte[] EncryptedAesIv => Data.EncryptedAesIv;
}

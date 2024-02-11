using System.Data.SQLite;
using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileResource(FileResource.ResourceManager manager, FileResource.ResourceData data) : Resource<FileResource.ResourceManager, FileResource.ResourceData, FileResource>(manager, data)
{
  public enum FileNodeType : byte { File, Folder, SymbolicLink }

  private const string NAME = "File";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileResource>.ResourceManager
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

    protected override FileResource NewResource(ResourceData data) => new(this, data);
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

    public FileResource Create(ResourceService.Transaction transaction, FileHubResource hub, FileResource? parentFolder, FileNodeType type, string name, UserAuthenticationResource.Token? token)
    {
      if (parentFolder != null && parentFolder.Type != FileNodeType.Folder)
      {
        throw new ArgumentException("Invalid parent folder node.", nameof(parentFolder));
      }

      if (Exists(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_HUB_ID, "=", hub.Id),
        new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", parentFolder?.Id),
        new WhereClause.CompareColumn(COLUMN_NAME, "=", name)
      )))
      {
        throw new ArgumentException("File exists.", nameof(name));
      }

      KeyService.AesPair hubAesPair;
      if (parentFolder == null)
      {
        if (token == null)
        {
          throw new ArgumentException("Invalid token.", nameof(token));
        }

        hubAesPair = hub.DecryptAesPair(token);
      }
      else
      {
        hubAesPair = DecryptAesPair(transaction, hub, parentFolder, token, FileAccessResource.AccessType.ReadWrite);
      }

      return Insert(transaction, new(
        (COLUMN_HUB_ID, hub.Id),
        (COLUMN_PARENT_FILE_ID, parentFolder?.Id),
        (COLUMN_TYPE, (byte)type),
        (COLUMN_NAME, name),

        (COLUMN_ENCRYPTED_AES_KEY, hubAesPair.Encrypt(RandomNumberGenerator.GetBytes(32))),
        (COLUMN_ENCRYPTED_AES_IV, hubAesPair.Encrypt(RandomNumberGenerator.GetBytes(16)))
      ));
    }

    public KeyService.AesPair DecryptAesPair(ResourceService.Transaction transaction, FileHubResource hub, FileResource? node, UserAuthenticationResource.Token? token, FileAccessResource.AccessType accessIntent)
    {
      hub.ThrowIfInvalid();
      node?.ThrowIfInvalid();
      token?.ThrowIfInvalid();

      return decryptAesPair(node);

      KeyService.AesPair decryptAesPair(FileResource? node)
      {
        if (node == null)
        {
          if (token == null)
          {
            throw new ArgumentException("Requires token to decrypt.", nameof(token));
          }

          return hub.DecryptAesPair(token);
        }

        if (hub.Id != node?.HubId)
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
            ?? throw new ArgumentException(token == null ? "Requires token to decrypt" : "No access granted for token.", nameof(token))
        ).DecryptAesPair(token);
      }
    }

    public FileResource GetRootFolder(ResourceService.Transaction transaction, FileHubResource fileHub, UserAuthenticationResource.Token token)
    {
      FileResource? root = fileHub.RootFolderId != null ? GetById(transaction, (long)fileHub.RootFolderId) : null;

      if (root == null)
      {
        Service.FileHubs.UpdateHubFolderIds(transaction, fileHub, (root = Create(transaction, fileHub, null, FileNodeType.Folder, "__ROOT", token)).Id, fileHub.TrashFolderId, fileHub.InternalFolderId);
      }

      return root;
    }

    public FileResource GetTrashFolder(ResourceService.Transaction transaction, FileHubResource fileHub, UserAuthenticationResource.Token token)
    {
      FileResource? trash = fileHub.RootFolderId != null ? GetById(transaction, (long)fileHub.RootFolderId) : null;

      if (trash == null)
      {
        Service.FileHubs.UpdateHubFolderIds(transaction, fileHub, fileHub.RootFolderId, (trash = Create(transaction, fileHub, null, FileNodeType.Folder, "__TRASH", token)).Id, fileHub.InternalFolderId);
      }

      return trash;
    }

    public FileResource GetInternalFolder(ResourceService.Transaction transaction, FileHubResource fileHub, UserAuthenticationResource.Token token)
    {
      FileResource? @internal = fileHub.RootFolderId != null ? GetById(transaction, (long)fileHub.RootFolderId) : null;

      if (@internal == null)
      {
        Service.FileHubs.UpdateHubFolderIds(transaction, fileHub, fileHub.RootFolderId, fileHub.TrashFolderId, (@internal = Create(transaction, fileHub, null, FileNodeType.Folder, "__INTERNAL", token)).Id);
      }

      return @internal;
    }

    public IEnumerable<FileResource> Scan(ResourceService.Transaction transaction, FileHubResource hub, FileResource? parentFolder, UserAuthenticationResource.Token? token, LimitClause? limitClause = null, OrderByClause? orderByClause = null, CancellationToken cancellationToken = default)
    {
      if (parentFolder != null && parentFolder.Type != FileNodeType.Folder)
      {
        throw new ArgumentException("Invalid file type.", nameof(parentFolder));
      }

      DecryptAesPair(transaction, hub, parentFolder, token, FileAccessResource.AccessType.Read);

      return Select(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", parentFolder?.Id),
        new WhereClause.CompareColumn(COLUMN_HUB_ID, "=", hub.Id)
      ), limitClause, orderByClause, cancellationToken);
    }

    public bool Move(ResourceService.Transaction transaction, FileHubResource hub, FileResource file, FileResource? newParentFolder, UserAuthenticationResource.Token? token, CancellationToken cancellationToken = default)
    {
      if (newParentFolder != null && newParentFolder.Type != FileNodeType.Folder)
      {
        throw new ArgumentException("Invalid destination type.", nameof(newParentFolder));
      }

      KeyService.AesPair pair = DecryptAesPair(transaction, hub, file, token, FileAccessResource.AccessType.None);
      KeyService.AesPair parentPair = DecryptAesPair(transaction, hub, newParentFolder, token, FileAccessResource.AccessType.ReadWrite);

      return Update(transaction, file, new SetClause(
        (COLUMN_PARENT_FILE_ID, newParentFolder?.Id),
        (COLUMN_ENCRYPTED_AES_KEY, parentPair.Encrypt(pair.Key)),
        (COLUMN_ENCRYPTED_AES_IV, parentPair.Encrypt(pair.Iv))
      ), cancellationToken);
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
  ) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long HubId => Data.HubId;
  public FileNodeType Type => Data.Type;
  public long? ParentFileId => Data.ParentFileId;
  public string Name => Data.Name;

  private byte[] EncryptedAesKey => Data.EncryptedAesKey;
  private byte[] EncryptedAesIv => Data.EncryptedAesIv;
}

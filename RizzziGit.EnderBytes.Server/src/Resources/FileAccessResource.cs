using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileAccessResource(FileAccessResource.ResourceManager manager, FileAccessResource.ResourceData data) : Resource<FileAccessResource.ResourceManager, FileAccessResource.ResourceData, FileAccessResource>(manager, data)
{
  public enum AccessType : byte { Manage, ReadWrite, Read }

  private const string NAME = "FileAccess";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceManager
  {
    private const string COLUMN_TARGET_USER_ID = "TargetUserId";
    private const string COLUMN_TARGET_NODE_ID = "TargetNodeId";
    private const string COLUMN_AES_KEY = "AesKey";
    private const string COLUMN_AES_IV = "AesIv";
    private const string COLUMN_TYPE = "Type";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Files, NAME, VERSION)
    {
      service.Files.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_NODE_ID, "=", resource.Id));
      service.Users.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_USER_ID, "=", resource.Id));
    }

    protected override FileAccessResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TARGET_USER_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_NODE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_AES_KEY)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_AES_IV)),

      (AccessType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_USER_ID} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_NODE_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_IV} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} integer null;");
      }
    }

    public FileAccessResource Create(ResourceService.Transaction transaction, FileNodeResource node, UserAuthenticationResource.Token token, UserResource? target, AccessType type)
    {
      node.ThrowIfInvalid();
      token.ThrowIfInvalid();

      KeyService.AesPair aesPair = node.Manager.DecryptAesPair(transaction, node, token);

      UserAuthenticationResource? userAuthentication = target != null
        ? Service.UserAuthentications.List(transaction, target, new(1)).FirstOrDefault()
          ?? throw new InvalidOperationException("Target user does not have authentication.")
        : null;

      return Insert(transaction, new(
        (COLUMN_TARGET_USER_ID, null),
        (COLUMN_TARGET_NODE_ID, node.Id),
        (COLUMN_AES_KEY, userAuthentication?.Encrypt(aesPair.Key) ?? aesPair.Key),
        (COLUMN_AES_IV, userAuthentication?.Encrypt(aesPair.Iv) ?? aesPair.Iv),
        (COLUMN_TYPE, (byte)type)
      ));
    }

    public FileAccessResource? GetByToken(ResourceService.Transaction transaction, UserAuthenticationResource.Token? token)
    {
      token?.ThrowIfInvalid();

      return SelectOne(transaction,
        token == null
          ? new WhereClause.CompareColumn(COLUMN_TARGET_NODE_ID, "=", null)
          : new WhereClause.Nested("or",
              new WhereClause.CompareColumn(COLUMN_TARGET_NODE_ID, "=", null),
              new WhereClause.CompareColumn(COLUMN_TARGET_NODE_ID, "=", token.UserId)
            )
      );
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long? TargetUserId,
    long TargetNodeId,

    byte[] AesKey,
    byte[] AesIv,

    AccessType Type
  ) : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long? TargetUserId => Data.TargetUserId;
  public long TargetNodeId => Data.TargetNodeId;

  private byte[] AesKey => Data.AesKey;
  private byte[] AesIv => Data.AesIv;

  public AccessType Type => Data.Type;

  public KeyService.AesPair DecryptAesPair(UserAuthenticationResource.Token? token)
  {
    if (TargetUserId == null)
    {
      return new(AesKey, AesIv);
    }

    if (token == null)
    {
      throw new ArgumentException("Requires user authentication token to decrypt pair.");
    }

    if (token.UserId != TargetUserId)
    {
      throw new ArgumentException("User authentication token does not belong to the target user.", nameof(token));
    }

    return new(token.Decrypt(AesKey), token.Decrypt(AesIv));
  }
}

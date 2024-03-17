using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Utilities;
using Services;

public sealed class FileAccessResource(FileAccessResource.ResourceManager manager, FileAccessResource.ResourceData data) : Resource<FileAccessResource.ResourceManager, FileAccessResource.ResourceData, FileAccessResource>(manager, data)
{
  public enum FileAccessTargetEntityType : byte { None, User }
  public enum FileAccessType : byte { ManageShares, ReadWrite, Read, None }

  public const string NAME = "FileAccess";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceManager
  {
    public const string COLUMN_TARGET_FILE_ID = "TargetFileId";
    public const string COLUMN_TARGET_ENTITY_ID = "TargetEntityId";
    public const string COLUMN_TARGET_ENTITY_TYPE = "TargetEntityType";
    public const string COLUMN_KEY = "AesKey";
    public const string COLUMN_TYPE = "Type";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Files.ResourceDeleted += (transaction, file, cancellationToken) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id), cancellationToken);
      Service.Users.ResourceDeleted += (transaction, user, cancellationToken) => Delete(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_TYPE, "=", (byte)FileAccessTargetEntityType.User),
        new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id)
      ), cancellationToken);
    }

    protected override FileAccessResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_FILE_ID)),
      reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_ENTITY_ID)),
      (FileAccessTargetEntityType)reader.GetByte(reader.GetOrdinal(COLUMN_TARGET_ENTITY_TYPE)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),
      (FileAccessType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_TYPE} tinyint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");
      }
    }

    public IEnumerable<FileAccessResource> List(ResourceService.Transaction transaction, FileResource file, LimitClause? limit = null, OrderByClause? orderBy = null, CancellationToken cancellationToken = default)
    {
      return Select(transaction, new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id), limit, orderBy, cancellationToken);
    }

    public FileAccessResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource targetFile, FileAccessType type, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (targetFile)
        {
          lock (userAuthenticationToken)
          {
            KeyService.AesPair fileKey = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, targetFile, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken).Key;

            return InsertAndGet(transaction, new(
              (COLUMN_TARGET_FILE_ID, targetFile.Id),
              (COLUMN_TARGET_ENTITY_ID, null),
              (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.None),
              (COLUMN_KEY, fileKey.Serialize()),
              (COLUMN_TYPE, (byte)type)
            ), cancellationToken);
          }
        }
      }
    }

    public FileAccessResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource targetFile, UserResource targetUser, FileAccessType type, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (targetFile)
        {
          targetFile.ThrowIfInvalid();
          targetFile.ThrowIfDoesNotBelongTo(storage);

          return userAuthenticationToken.Enter(() =>
          {
            userAuthenticationToken.ThrowIfInvalid();

            KeyService.AesPair fileKey = transaction.ResoruceService.Storages.DecryptKey(transaction, storage, targetFile, userAuthenticationToken, FileAccessType.ReadWrite, cancellationToken).Key;

            return InsertAndGet(transaction, new(
              (COLUMN_TARGET_FILE_ID, targetFile.Id),
              (COLUMN_TARGET_ENTITY_ID, targetUser.Id),
              (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.User),
              (COLUMN_KEY, targetUser.Encrypt(fileKey.Serialize())),
              (COLUMN_TYPE, (byte)type)
            ), cancellationToken);
          });
        }
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,
    long TargetFileId,
    long TargetEntityId,
    FileAccessTargetEntityType TargetEntityType,
    byte[] AesKey,
    FileAccessType Type
  ) : Resource<ResourceManager, ResourceData, FileAccessResource>.ResourceData(Id, CreateTime, UpdateTime);

  public long TargetFileId => Data.TargetFileId;
  public long TargetEntityId => Data.TargetEntityId;
  public FileAccessTargetEntityType TargetEntityType => Data.TargetEntityType;
  public byte[] Key => Data.AesKey;
  public FileAccessType Type => Data.Type;
}

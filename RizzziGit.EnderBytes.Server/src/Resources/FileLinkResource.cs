using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using System.Text;
using Services;
using Utilities;

public sealed partial class FileLinkResource(FileLinkResource.ResourceManager manager, FileLinkResource.ResourceData data) : Resource<FileLinkResource.ResourceManager, FileLinkResource.ResourceData, FileLinkResource>(manager, data)
{
  public const string NAME = "FileLink";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceManager
  {
    public const string COLUMN_FILE_ID = "FileId";
    public const string COLUMN_TARGET_PATH = "TargetPath";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Files.ResourceDeleted += (transaction, resource, cancellationToken) =>
      {
        if (resource.Type == FileResource.FileType.SymbolicLink)
        {
          SqlNonQuery(transaction, $"delete from {NAME} where {COLUMN_FILE_ID} = {{0}}", resource.Id);
        }
      };
    }

    protected override FileLinkResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_FILE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_TARGET_PATH))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_FILE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TARGET_PATH} blob not null;");
      }
    }

    public FileLinkResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource file, string targetPath, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = default, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (file)
        {
          if (file.Type != FileResource.FileType.SymbolicLink)
          {
            throw new ArgumentException("Not a symbolic link.", nameof(file));
          }

          StorageResource.DecryptedKeyInfo decryptedKey = Service.Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

          return InsertAndGet(transaction, new(
            (COLUMN_FILE_ID, file.Id),
            (COLUMN_TARGET_PATH, decryptedKey.Key.Encrypt(Encoding.UTF8.GetBytes(targetPath)))
          ), cancellationToken);
        }
      }
    }

    public bool SetLink(ResourceService.Transaction transaction, FileLinkResource fileLink, string targetPath, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = default, CancellationToken cancellationToken = default)
    {
      lock (fileLink)
      {
        fileLink.ThrowIfInvalid();

        return Update(transaction, fileLink, new((COLUMN_TARGET_PATH, targetPath)), cancellationToken);
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, long FileId, byte[] Path) : Resource<ResourceManager, ResourceData, FileLinkResource>.ResourceData(Id, CreateTime, UpdateTime);
}

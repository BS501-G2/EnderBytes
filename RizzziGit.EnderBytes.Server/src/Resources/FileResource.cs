using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

public sealed class FileResource(FileResource.ResourceManager manager, FileResource.ResourceData data) : Resource<FileResource.ResourceManager, FileResource.ResourceData, FileResource>(manager, data)
{
  private const string NAME = "File";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileResource>.ResourceManager
  {
    private const string COLUMN_HUB_ID = "HubId";
    private const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    private const string COLUMN_NAME = "Name";
    private const string COLUMN_AES_KEY = "AesKey";

    public ResourceManager(ResourceService service) : base(service, ResourceService.Scope.Main, NAME, VERSION)
    {
      service.FileHubs.ResourceDeleted += (transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_HUB_ID, "=", resource.Id));
    }

    protected override FileResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(SQLiteDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_HUB_ID)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_FILE_ID)),
      reader.GetString(reader.GetOrdinal(COLUMN_NAME)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_AES_KEY))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_HUB_ID} integer not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_FILE_ID} integer null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_KEY} blob not null;");
      }
    }
  }

  public new sealed record ResourceData(
    long Id,
    long CreateTime,
    long UpdateTime,

    long HubId,
    long? ParentFileId,
    string Name,
    byte[] AesKey
  ) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);
}

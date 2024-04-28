namespace RizzziGit.EnderBytes.Resources;

using System.Data.Common;
using System.Threading.Tasks;
using RizzziGit.EnderBytes.Services;
using ResourceManager = ResourceManager<FileBlobManager, FileBlobManager.Resource>;

public sealed class FileBlobManager : ResourceManager
{
  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "FileBlob";
  public const int VERSION = 1;

  public const string COLUMN_FILE_ID = "FileId";
  public const string COLUMN_BLOB = "BlobData";

  public FileBlobManager(ResourceService service, string name, int version) : base(service, name, version)
  {
    GetManager<FileManager>().RegisterDeleteHandler((transaction, file) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_FILE_ID, "=", file.Id)));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime)
  {
    throw new NotImplementedException();
  }

  protected override Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    throw new NotImplementedException();
  }
}

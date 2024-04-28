using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileManager, FileManager.Resource>;

public sealed partial class FileManager : ResourceManager
{
  public const string NAME = "File";
  public const int VERSION = 1;

  private const string COLUMN_TRASH_TIME = "TrashTime";
  private const string COLUMN_DOMAIN_USER_ID = "DomainUserId";
  private const string COLUMN_AUTHOR_USER_ID = "AuthorUserId";
  private const string COLUMN_PARENT_ID = "ParentId";
  private const string COLUMN_NAME = "Name";
  private const string COLUMN_IS_FOLDER = "IsFolder";
  private const string COLUMN_AES_KEY = "EncryptedPassword";

  public new sealed record Resource(
    long Id,
    long CreateTime,
    long UpdateTime,

    long? TrashTime,

    long DomainUserId,
    long AuthorUserId,

    long? ParentId,
    string Name,

    bool IsFolder,

    string EncryptedPassword
  ) : ResourceManager.Resource(Id, CreateTime, UpdateTime)
  {
    [JsonIgnore]
    public readonly string EncryptedPassword = EncryptedPassword;
  }

  public sealed class NotAFolderException(Resource file) : Exception($"#{file.Id} is not a folder.")
  {
    public readonly Resource File = file;
  }

  public sealed class NotAFileException(Resource file) : Exception($"#{file.Id} is not a file.")
  {
    public readonly Resource File = file;
  }

  public sealed class InvalidTrashOperationException(Resource file) : Exception($"#{file.Id} is a root folder and cannot be moved to trash.");
  public sealed class InvalidMoveException(Resource file, Resource newParent) : Exception($"#{file.Id} cannot be moved to #{newParent.Id} because the file is already in that folder.");

  public FileManager(ResourceService service) : base(service, NAME, VERSION)
  {
    GetManager<UserManager>().RegisterDeleteHandler((transaction, user) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_AUTHOR_USER_ID, "=", user.Id)));
    RegisterDeleteHandler((transaction, resource) => Delete(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", resource.Id)));
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TRASH_TIME)),

    reader.GetInt64(reader.GetOrdinal(COLUMN_DOMAIN_USER_ID)),
    reader.GetInt64(reader.GetOrdinal(COLUMN_AUTHOR_USER_ID)),
    reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_ID)),
    reader.GetString(reader.GetOrdinal(COLUMN_NAME)),
    reader.GetBoolean(reader.GetOrdinal(COLUMN_IS_FOLDER)),
    reader.GetString(reader.GetOrdinal(COLUMN_AES_KEY))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TRASH_TIME} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_DOMAIN_USER_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AUTHOR_USER_ID} bigint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_ID} bigint null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null collate utf8mb4_unicode_ci;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_IS_FOLDER} tinyint(1) not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_AES_KEY} varchar(128) not null;");
    }
  }

  public async Task<Resource> GetRootFromUser(ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken)
  {
    Resource? resource;

    if ((resource = await SelectFirst
    (transaction, new WhereClause.CompareColumn(COLUMN_AUTHOR_USER_ID, "=", userAuthenticationToken.UserId))) == null)
    {
      KeyService.AesPair newKey = Service.Server.KeyService.GetNewAesPair();

      resource = await InsertAndGet(transaction, new(
        (COLUMN_DOMAIN_USER_ID, userAuthenticationToken.UserId),
        (COLUMN_AUTHOR_USER_ID, userAuthenticationToken.UserId),
        (COLUMN_PARENT_ID, null),
        (COLUMN_NAME, "/"),
        (COLUMN_IS_FOLDER, true),
        (COLUMN_AES_KEY, userAuthenticationToken.Encrypt(newKey.Serialize()))
      ));
    }

    return resource;
  }

  public async Task<Resource[]> PathChain(ResourceService.Transaction transaction, Resource file)
  {
    List<Resource> files = [];

    Resource? resource = file;
    while (resource.ParentId != null)
    {
      if ((resource = await GetById(transaction, (long)resource.ParentId)) == null)
      {
        break;
      }

      files.Add(resource);
    }

    return [.. files];
  }

  public async Task<Resource> Create(ResourceService.Transaction transaction, Resource parentFolder, string name, bool isFolder, UserAuthenticationToken userAuthenticationToken)
  {
    if (!parentFolder.IsFolder)
    {
      throw new NotAFolderException(parentFolder);
    }

    KeyService.AesPair newKey = Service.Server.KeyService.GetNewAesPair();
    return await InsertAndGet(transaction, new(
      (COLUMN_AUTHOR_USER_ID, userAuthenticationToken.UserId),
      (COLUMN_PARENT_ID, parentFolder.Id),
      (COLUMN_NAME, await ThrowIfInvalidName(transaction, parentFolder, name)),
      (COLUMN_IS_FOLDER, isFolder),
      (COLUMN_AES_KEY, userAuthenticationToken.Encrypt(newKey.Serialize()))
    ));
  }

  public async Task<bool> Move(ResourceService.Transaction transaction, Resource file, Resource newParent, string? newName = null)
  {
    if (!newParent.IsFolder)
    {
      throw new NotAFolderException(newParent);
    }

    Resource[] pathChain = await PathChain(transaction, newParent);

    if (file.Id == newParent.Id || pathChain.Any((fileTest) => fileTest.Id == file.Id))
    {
      throw new InvalidMoveException(file, newParent);
    }

    if (await Count(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_TRASH_TIME, "is", null),
      new WhereClause.CompareColumn(COLUMN_NAME, "=", newName ?? file.Name)
    )) > 0)
    {
      int currentNameIter = 1;
      string currentName() => $"{file.Name} ({currentNameIter})";

      while (await Count(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_TRASH_TIME, "is", null),
        new WhereClause.CompareColumn(COLUMN_NAME, "=", currentName())
      )) > 0)
      {
        currentNameIter++;
      }

      return await Update(transaction, file, new(
        (COLUMN_TRASH_TIME, null),
        (COLUMN_NAME, newName ?? currentName())
      ));
    }

    return await Update(transaction, file, new(
      (COLUMN_PARENT_ID, newParent.Id),
      (COLUMN_NAME, newName ?? file.Name)
    ));
  }

  public async Task<Resource[]> List(ResourceService.Transaction transaction, Resource folder, LimitClause? limitClause = null, OrderByClause? orderByClause = null)
  {
    if (!folder.IsFolder)
    {
      throw new NotAFolderException(folder);
    }

    return await Select(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", folder.Id), limitClause, orderByClause);
  }

  public async Task<bool> ListTrashed(ResourceService.Transaction transaction, UserManager.Resource user, LimitClause? limitClause = null, OrderByClause? orderByClause = null)
  {
    return (await Select(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_DOMAIN_USER_ID, "=", user.Id),
      new WhereClause.CompareColumn(COLUMN_TRASH_TIME, "is not", null)
    ), limitClause, orderByClause)).Length != 0;
  }

  public async Task<bool> Restore(ResourceService.Transaction transaction, Resource file)
  {
    if (file.TrashTime == null)
    {
      return false;
    }

    if (await Count(transaction, new WhereClause.Nested("and",
      new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.Id),
      new WhereClause.CompareColumn(COLUMN_TRASH_TIME, "is", null),
      new WhereClause.CompareColumn(COLUMN_NAME, "=", file.Name)
    )) > 0)
    {
      int currentNameIter = 1;
      string currentName() => $"{file.Name} ({currentNameIter})";

      while (await Count(transaction, new WhereClause.Nested("and",
        new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.Id),
        new WhereClause.CompareColumn(COLUMN_TRASH_TIME, "is", null),
        new WhereClause.CompareColumn(COLUMN_NAME, "=", currentName())
      )) > 0)
      {
        currentNameIter++;
      }

      return await Update(transaction, file, new(
        (COLUMN_TRASH_TIME, null),
        (COLUMN_NAME, currentName())
      ));
    }

    return await Update(transaction, file, new(
      (COLUMN_TRASH_TIME, null)
    ));

  }

  public async Task<bool> Trash(ResourceService.Transaction transaction, Resource file)
  {
    if (file.TrashTime != null)
    {
      return false;
    }
    else if (file.ParentId == null)
    {
      throw new InvalidTrashOperationException(file);
    }

    return await Update(transaction, file, new(
      (COLUMN_TRASH_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
    ));
  }
}

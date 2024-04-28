using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

using BaseManager = ResourceManager<UserContactManager, UserContactManager.Resource>;

public enum UserContactType { Email, Phone }

public sealed class UserContactManager : BaseManager
{
  public new sealed record Resource(long Id, long CreateTime, long UpdateTime, UserContactType Type, bool IsVerified) : BaseManager.Resource(Id, CreateTime, UpdateTime);

  public const string NAME = "UserContact";
  public const int VERSION = 1;

  private const string COLUMN_TYPE = "Type";
  private const string COLUMN_IS_VERIFIED = "IsVerified";

  public UserContactManager(ResourceService service) : base(service, NAME, VERSION)
  {
  }

  protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
    id, createTime, updateTime,

    (UserContactType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE)),
    reader.GetBoolean(reader.GetOrdinal(COLUMN_IS_VERIFIED))
  );

  protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
  {
    if (oldVersion < 1)
    {
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");
      await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_IS_VERIFIED} tinyint(1) not null;");
    }
  }
}

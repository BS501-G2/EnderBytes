using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Services;

public sealed class UserAuthenticationSessionTokenResource(UserAuthenticationSessionTokenResource.ResourceManager manager, UserAuthenticationSessionTokenResource.ResourceData data) : Resource<UserAuthenticationSessionTokenResource.ResourceManager, UserAuthenticationSessionTokenResource.ResourceData, UserAuthenticationSessionTokenResource>(manager, data)
{
  public const string NAME = "UserAuthenticationSessionToken";
  public const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, UserAuthenticationSessionTokenResource>.ResourceManager
  {
    public const string COLUMN_USER_AUTHENTICATION_ID = "";

    public ResourceManager(ResourceService service, string name, int version) : base(service, name, version)
    {

    }

    protected override UserAuthenticationSessionTokenResource NewResource(ResourceData data) => new(this, data);
    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {

      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime) : Resource<ResourceManager, ResourceData, UserAuthenticationSessionTokenResource>.ResourceData(Id, CreateTime, UpdateTime);
}

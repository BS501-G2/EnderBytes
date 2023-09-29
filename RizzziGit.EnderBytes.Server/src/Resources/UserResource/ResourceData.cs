using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

public sealed partial class UserResource
{
  public const string JSON_KEY_USERNAME = "username";

  public new sealed partial class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    string username
  ) : Resource<ResourceManager, ResourceData, UserResource>.ResourceData(id, createTime, updateTime)
  {
    public string Username = username;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      Username = data.Username;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_USERNAME, Username }
      });

      return jObject;
    }
  }

  public string Username => Data.Username;
}

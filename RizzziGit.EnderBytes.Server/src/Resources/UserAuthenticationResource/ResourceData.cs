using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

public sealed partial class UserAuthenticationResource
{
  public const string JSON_KEY_USER_ID = "userId";
  public const string JSON_KEY_TYPE = "type";

  public new class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    ulong userId,
    byte type,
    byte[] payload
  ) : Resource<ResourceManager, ResourceData, UserAuthenticationResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong UserID = userId;
    public byte Type = type;
    public byte[] Payload = payload;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      UserID = data.UserID;
      Type = data.Type;
      Payload = data.Payload;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_USER_ID, UserID },
        { JSON_KEY_TYPE, Type }
      });

      return jObject;
    }
  }

  public ulong UserID => Data.UserID;
  public byte Type => Data.Type;
  public byte[] Payload => Data.Payload;
}

using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>
{
  public const string JSON_KEY_ID = "id";
  public const string JSON_KEY_CREATE_TIME = "createTime";
  public const string JSON_KEY_UPDATE_TIME = "updateTime";

  public abstract class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime
  ) : IResourceData
  {
    public ulong ID = id;
    public ulong CreateTime = createTime;
    public ulong UpdateTime = updateTime;

    public virtual void CopyFrom(D data)
    {
      if (this == data)
      {
        return;
      }

      if (ID != data.ID)
      {
        throw new InvalidOperationException("Inconsistent data ID.");
      }

      CreateTime = data.CreateTime;
      UpdateTime = data.UpdateTime;
    }

    public virtual JObject ToJSON() => new()
    {
      { JSON_KEY_ID, ID },
      { JSON_KEY_CREATE_TIME, CreateTime },
      { JSON_KEY_UPDATE_TIME, UpdateTime }
    };
  }

  public ulong ID => Data.ID;
  public ulong CreateTime => Data.CreateTime;
  public ulong UpdateTime => Data.UpdateTime;

  public JObject ToJSON() => Data.ToJSON();
  private void UpdateData(D data) => Data.CopyFrom(data);
}

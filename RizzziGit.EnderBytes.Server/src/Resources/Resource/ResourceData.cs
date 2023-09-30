namespace RizzziGit.EnderBytes.Resources;

using Buffer;

public abstract partial class Resource<M, D, R>
{
  public new abstract partial class ResourceData : Shared.Resources.Resource<M, D, R>.ResourceData, Shared.Resources.IResourceData
  {
    protected ResourceData(ulong id, ulong createTime, ulong updateTime) : base(id, createTime, updateTime)
    {
    }
  }

  public string IDHex => Buffer.From(ID).ToHexString();
}

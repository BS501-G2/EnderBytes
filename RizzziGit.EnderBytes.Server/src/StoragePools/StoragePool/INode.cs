namespace RizzziGit.EnderBytes.StoragePools;

using Connections;
using Resources;

public abstract partial class StoragePool
{
  public partial interface INode
  {
    public StoragePool Pool { get; }

    public long Id { get; }
    public long CreateTime { get; }
    public long AccessTime { get; }
    public long ModifyTime { get; }

    public long? UserId { get; }
    public long KeySharedId { get; }
    public string Name { get; }

    Task<IFolder?> IGetParent(KeyResource.Transformer transformer);

    public async Task<Path> GetPath(KeyResource.Transformer transformer)
    {
      IFolder? folder = await IGetParent(transformer);
      string[] path = folder == null ? [Name] : [..await folder.GetPath(transformer), Name];

      return new(path);
    }
  }
}

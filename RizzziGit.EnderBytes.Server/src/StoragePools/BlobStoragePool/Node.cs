namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Buffer;
using Connections;
using System.Threading.Tasks;

public sealed partial class BlobStoragePool
{
  public abstract partial class Node : IBlobNode
  {
    private Node(BlobStoragePool pool, BlobNodeResource resource)
    {
      Pool = pool;
      Resource = resource;
    }

    protected readonly BlobStoragePool Pool;
    protected readonly BlobNodeResource Resource;
    protected Database Database => Pool.Database;
    protected Resources.BlobStorage.ResourceManager ResourceManager => Resource.Manager.Main;
    protected Server Server => ResourceManager.Server;

    BlobStoragePool IBlobNode.Pool => Pool;
    BlobNodeResource IBlobNode.Resource => Resource;
    Database IBlobClass.Database => Database;
    Resources.BlobStorage.ResourceManager IBlobClass.ResourceManager => ResourceManager;
    Server IBlobClass.Server => Pool.Manager.Server;

    long INode.Id => Resource.Id;
    long INode.CreateTime => Resource.CreateTime;
    long INode.AccessTime => Resource.AccessTime;
    long INode.ModifyTime => Resource.UpdateTime;
    long? INode.UserId => Pool.Resource.UserId;
    long INode.KeySharedId => Resource.KeySharedId;
    string INode.Name => Resource.Name;

    Task<INode.IFolder?> INode.IGetParent(KeyResource.Transformer transformer)
    {
      throw new NotImplementedException();
    }

    // async Task<INode.IFolder?> INode.IGetParent(Connection connection)
    // {
    //   if (Resource.ParentId == null)
    //   {
    //     return null;
    //   }

    //   BlobNodeResource? resource = await Pool.Database.RunTransaction((transaction) => Pool.ResourceManager.Nodes.GetById(transaction, (long)Resource.ParentId));
    //   if (resource == null)
    //   {
    //     return null;
    //   }

    //   return (INode.IFolder)Pool.ResolveNode(resource);
    // }
  }
}

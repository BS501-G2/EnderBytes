namespace RizzziGit.EnderBytes.StoragePools;

using Resources;

public abstract partial class StoragePool
{
  public abstract partial class Node
  {
    public abstract class Folder : Node
    {
      protected abstract Task<Node[]> Internal_Scan(KeyResource.Transformer transformer);

      public Task<Node[]> Scan(KeyResource.Transformer transformer) => Internal_Scan(transformer);
    }
  }
}

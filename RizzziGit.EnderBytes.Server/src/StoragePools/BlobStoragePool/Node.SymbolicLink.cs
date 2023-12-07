namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;

public sealed partial class BlobStoragePool
{
  public abstract partial class Node
  {
    public sealed class SymbolicLink : Node, IBlobSymbolicLink
    {
      public SymbolicLink(BlobStoragePool pool, BlobNodeResource resource) : base(pool, resource)
      {
      }
    }
  }
}

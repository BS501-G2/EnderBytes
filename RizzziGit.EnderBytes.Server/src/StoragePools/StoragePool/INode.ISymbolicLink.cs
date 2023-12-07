namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public partial interface INode
  {
    public interface ISymbolicLink : INode
    {
    }
  }
}

namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R> : Shared.Resources.Resource<M, D, R>, Shared.Resources.IResource
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  protected Resource(M manager, D data) : base(manager, data)
  {
  }
}

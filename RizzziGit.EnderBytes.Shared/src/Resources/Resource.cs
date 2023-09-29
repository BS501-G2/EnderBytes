namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>(M manager, D data) : IResource
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  protected readonly M Manager = manager;
  protected readonly D Data = data;
}

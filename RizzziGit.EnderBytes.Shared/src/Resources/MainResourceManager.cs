namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class MainResourceManager()
{
  private async Task InitResourceManagers(CancellationToken cancellationToken)
  {
    List<Task> initTasks = [];
    foreach (WeakReference<IResourceManager> manager in ResourceManagers.Values)
    {
      if (manager.TryGetTarget(out IResourceManager? target))
      {
        initTasks.Add(target.Init(cancellationToken));
      }
    }

    await Task.WhenAll(initTasks);
  }

  public async virtual Task Init(CancellationToken cancellationToken)
  {
    await InitResourceManagers(cancellationToken);
  }
}

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

public sealed partial class FileService(Server server) : Server.SubService(server, "Blobs")
{
  private readonly WeakDictionary<FileHubResource, Hub> Hubs = [];

  public bool IsHubValid(FileHubResource resource, Hub hub)
  {
    lock (this)
    {
      return resource.IsValid
        && Hubs.TryGetValue(resource, out Hub? testHandle)
        && testHandle == hub;
    }
  }

  public void ThrowIfHubInvalid(FileHubResource resource, Hub hub)
  {
    if (!IsHubValid(resource, hub))
    {
      throw new ArgumentException("Invalid hub.", nameof(hub));
    }
  }

  public Hub GetFileHubHandle(FileHubResource resource)
  {
    resource.ThrowIfInvalid();

    lock (this)
    {
      if (!Hubs.TryGetValue(resource, out Hub? hub))
      {
        Hubs.Add(resource, hub = new(this, resource));
      }

      return hub;
    }
  }

  protected override Task OnStop(Exception? exception = null)
  {
    lock (this)
    {
      Hubs.Clear();
    }

    return Task.CompletedTask;
  }
}

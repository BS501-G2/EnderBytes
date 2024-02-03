namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

public sealed partial class FileService : Server.SubService
{
  public FileService(Server server) : base(server, "Files")
  {
    server.ResourceService.FileHubs.ResourceDeleted += (transaction, resource) =>
    {
      lock (this)
      {
        if (Hubs.TryGetValue(resource, out Hub? hub))
        {
          Hubs.Remove(resource);
          transaction.RegisterOnFailureHandler(() => Hubs.Add(resource, hub));
        }
      }
    };
  }

  private readonly WeakDictionary<FileHubResource, Hub> Hubs = [];

  public bool IsHubValid(FileHubResource resource, Hub hub)
  {
    lock (this)
    {
      lock (resource)
      {
        lock (hub)
        {
          return resource.IsValid
            && Hubs.TryGetValue(resource, out Hub? testHandle)
            && testHandle == hub;
        }
      }
    }
  }

  public void ThrowIfHubInvalid(FileHubResource resource, Hub hub)
  {
    if (!IsHubValid(resource, hub))
    {
      throw new ArgumentException("Invalid hub.", nameof(hub));
    }
  }

  public async Task<Hub> Get(long hubId)
  {
    FileHubResource resource = await Server.ResourceService.Transact(ResourceService.Scope.Files, (transaction, cancellationToken) => Server.ResourceService.FileHubs.GetById(transaction, hubId))
      ?? throw new ArgumentException("Invalid hub id.", nameof(hubId));

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

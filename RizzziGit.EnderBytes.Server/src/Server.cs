namespace RizzziGit.EnderBytes.Core;

using Framework.Services;
using Services;

public sealed class Server : Service
{
  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }

  public Server(string? workingPath = null) : base("Server")
  {
    WorkingPath = workingPath ?? Path.Join(Environment.CurrentDirectory, ".EnderBytes");
    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }

    KeyService = new(this);
    ResourceService = new(this);
  }

  public readonly string? WorkingPath;

  public readonly KeyService KeyService;
  public readonly ResourceService ResourceService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await KeyService.Start(cancellationToken);
    await ResourceService.Start(cancellationToken);

    await base.OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken) => await WatchDog([KeyService, ResourceService], cancellationToken);

  protected override async Task OnStop(Exception? exception)
  {
    await ResourceService.Stop();
    await KeyService.Stop();

    await base.OnStop(exception);
  }
}

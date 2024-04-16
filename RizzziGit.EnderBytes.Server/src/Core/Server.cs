namespace RizzziGit.EnderBytes.Core;

using Commons.Services;

using Services;

public sealed partial class Server : Service
{
  public Server(ServerConfiguration configuration) : base("Server")
  {
    Configuration = configuration;

    KeyService = new(this);
    ResourceService = new(this);
    WebService = new(this);

    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
  }

  public readonly ServerConfiguration Configuration;
  public string WorkingPath => Configuration.WorkingPath;

  public readonly KeyService KeyService;
  public readonly ResourceService ResourceService;
  public readonly WebService WebService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await KeyService.Start(cancellationToken);
    await ResourceService.Start(cancellationToken);
    await WebService.Start(cancellationToken);

    await base.OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await WatchDog([KeyService, ResourceService, WebService], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await WebService.Stop();
    await ResourceService.Stop();
    await KeyService.Stop();

    await base.OnStop(exception);
  }
}

namespace RizzziGit.EnderBytes.Core;

using Framework.Services;

using Services;

public sealed partial class Server : Service
{
  public Server(ServerConfiguration? configuration = null) : base("Server")
  {
    Configuration = configuration ?? new();

    KeyService = new(this);
    ResourceService = new(this);
    ConnectionService = new(this);
    SessionService = new(this);
    ProtocolService = new(this);

    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
  }

  public readonly ServerConfiguration Configuration;
  public string WorkingPath => Configuration.WorkingPath;

  public readonly KeyService KeyService;
  public readonly ResourceService ResourceService;
  public readonly ConnectionService ConnectionService;
  public readonly SessionService SessionService;
  public readonly ProtocolService ProtocolService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await KeyService.Start(cancellationToken);
    await ResourceService.Start(cancellationToken);
    await ConnectionService.Start(cancellationToken);
    await SessionService.Start(cancellationToken);
    await ProtocolService.Start(cancellationToken);

    await base.OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await WatchDog([KeyService, ResourceService, ConnectionService, SessionService, ProtocolService], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ProtocolService.Stop();
    await SessionService.Stop();
    await ConnectionService.Stop();
    await ResourceService.Stop();
    await KeyService.Stop();

    await base.OnStop(exception);
  }
}

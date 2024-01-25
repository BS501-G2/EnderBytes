namespace RizzziGit.EnderBytes.Core;

using Framework.Services;
using Services;

public sealed record ServerConfiguration(
  string? WorkingPath = null,

  int KeyGeneratorThreads = 4,
  int MaxPregeneratedKeyCount = 1000
)
{
  public string WorkingPath = WorkingPath ?? Path.Join(Environment.CurrentDirectory, ".EnderBytes");
}

public sealed class Server : Service
{
  public abstract class SubService(Server server, string name) : Service(name, server)
  {
    public readonly Server Server = server;
  }

  public Server(ServerConfiguration? configuration = null) : base("Server")
  {
    Configuration = configuration ?? new();

    KeyService = new(this);
    ResourceService = new(this);

    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
  }

  public readonly ServerConfiguration Configuration;
  public string WorkingPath => Configuration.WorkingPath;

  public readonly KeyService KeyService;
  public readonly ResourceService ResourceService;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await KeyService.Start(cancellationToken);
    await ResourceService.Start(cancellationToken);

    await base.OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await WatchDog([KeyService, ResourceService], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await ResourceService.Stop();
    await KeyService.Stop();

    await base.OnStop(exception);
  }
}

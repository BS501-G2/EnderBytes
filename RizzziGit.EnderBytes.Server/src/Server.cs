namespace RizzziGit.EnderBytes;

using Resources;
using RizzziGit.EnderBytes.Connections;
using RizzziGit.EnderBytes.Sessions;

public struct ServerConfig()
{
  public string DatabasePath = Path.Join(Environment.CurrentDirectory, ".db");
  public int DefaultUserAuthenticationResourceIterationCount = 1000;
}

public sealed class Server : Service
{
  public Server() : this(new()) { }
  public Server(ServerConfig config) : base("Server")
  {
    Config = config;
    Resources = new(this);
    Sessions = new(this);
    Connections = new(this);
  }

  public readonly ServerConfig Config;
  public readonly MainResourceManager Resources;
  public readonly SessionManager Sessions;
  public readonly ConnectionManager Connections;

  private async void WatchDogInternal(Task task, CancellationToken cancellationToken)
  {
    await task.WaitAsync(cancellationToken);

    if (task.IsFaulted)
    {
      _ = Stop();
    }
  }

  private Task WatchDog(Task task, CancellationToken cancellationToken)
  {
    WatchDogInternal(task, cancellationToken);

    return task;
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await WatchDog(Resources.Start(), cancellationToken);
    await WatchDog(Sessions.Start(), cancellationToken);
    await WatchDog(Connections.Start(), cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await Task.Delay(-1, cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Connections.Stop();
    await Sessions.Stop();
    await Resources.Stop();
  }
}

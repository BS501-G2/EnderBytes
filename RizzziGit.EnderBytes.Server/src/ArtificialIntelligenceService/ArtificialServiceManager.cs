
namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed class ArtificialIntelligenceManager : Service
{
  public ArtificialIntelligenceManager(Server server) : base("AI", server)
  {
    Server = server;
    Whisper = new(this);
  }

  public readonly Server Server;
  public readonly WhisperAI Whisper;

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return WatchDog([Whisper], cancellationToken);
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Whisper.Start();
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Whisper.Stop();
  }
}
namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed class ArtificialIntelligenceManager : Service
{
  public ArtificialIntelligenceManager(Server server) : base("AI", server)
  {
    Server = server;

    #if WHISPER_CPP
    Whisper = new WhisperAiCpp(this);
    #elif WHISPER_PYTHON
    Whisper = new WhisperAiPy(this);
    #else
    throw new InvalidOperationException("Does not know which version of WhisperAI to use.");
    #endif
  }

  public readonly Server Server;
  public readonly WhisperAi Whisper;

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

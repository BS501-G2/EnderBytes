namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed class WhisperAiPy(ArtificialIntelligenceManager manager) : WhisperAi(manager, "Whisper (Python)")
{
  protected override Task OnRun(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task OnStop(Exception? exception)
  {
    throw new NotImplementedException();
  }

  public override IAsyncEnumerable<TranscriptBlock> Transcribe(Stream stream, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}

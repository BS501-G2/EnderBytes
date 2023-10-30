namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed record TranscriptBlock(string Text, string Language, long Timecode, long Duration);

public abstract class WhisperAi(ArtificialIntelligenceManager manager, string name) : Service(name, manager)
{
  public readonly ArtificialIntelligenceManager Manager = manager;

  public abstract IAsyncEnumerable<TranscriptBlock> Transcribe(Stream stream, CancellationToken cancellationToken);
}

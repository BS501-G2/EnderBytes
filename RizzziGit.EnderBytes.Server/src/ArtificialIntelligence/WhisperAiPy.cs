using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed class WhisperAiPy(ArtificialIntelligenceManager manager) : WhisperAi(manager, "Whisper (Python)")
{
  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    // ScriptEngine engine = Python.CreateEngine();
    // ScriptScope scope = engine.CreateScope();

    // engine.SetSearchPaths([
    //   ..engine.GetSearchPaths(),

    //   Path.Join(Environment.GetEnvironmentVariable("HOME"), ".local/lib/python3.11/sites-packages"),
    //   Path.Join("/usr/lib/python3.11/site-packages"),
    //   Path.Join("/usr/lib/python3.11")
    // ]);

    // engine.Execute("import whisper");

    throw new NotImplementedException();
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
  }

  protected override async Task OnStop(Exception? exception)
  {
  }

  public override async IAsyncEnumerable<TranscriptBlock> Transcribe(Stream stream, CancellationToken cancellationToken)
  {
    yield break;
  }
}

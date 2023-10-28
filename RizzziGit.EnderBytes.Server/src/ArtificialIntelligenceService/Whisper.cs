
using Whisper.net;

namespace RizzziGit.EnderBytes.ArtificialIntelligence;

public sealed class WhisperAI : Service
{
  public WhisperAI(ArtificialIntelligenceManager manager) : base("Whisper", manager)
  {
    Manager = manager;
  }

  public readonly ArtificialIntelligenceManager Manager;
  private WhisperFactory? Factory;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    Factory = WhisperFactory.FromPath(Manager.Server.Configuration.WhisperDatasetPath);
    WhisperProcessor processor = Factory
      .CreateBuilder()
      .WithLanguage("auto")
      .Build();

    using FileStream stream = File.OpenRead("/media/cool/AC233/leeknow.wav");

    await foreach (var result in processor.ProcessAsync(stream, cancellationToken))
    {
      Console.WriteLine($"{result.Text}");
    }
  }

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  protected override Task OnStop(Exception? exception)
  {
    return Task.CompletedTask;
  }
}
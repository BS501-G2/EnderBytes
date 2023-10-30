using Whisper.net;
using Whisper.net.Ggml;
using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.ArtificialIntelligence;

using Collections;
using Extensions;

public sealed class WhisperAiCpp(ArtificialIntelligenceManager manager) : WhisperAi(manager, "Whisper (C++)")
{
  private WhisperProcessor? Processor;
  private WaitQueue<(TaskCompletionSource<IAsyncEnumerable<TranscriptBlock>> source, Stream readStream, CancellationToken cancellationToken)>? WaitQueue;

  public bool IsAvailable => Processor != null;

  public override async IAsyncEnumerable<TranscriptBlock> Transcribe(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    TaskCompletionSource<IAsyncEnumerable<TranscriptBlock>> source = new();
    await WaitQueue!.Enqueue((source, stream, cancellationToken), cancellationToken);
    await foreach (TranscriptBlock entry in await source.Task)
    {
      yield return entry;
    }
  }

  protected override async Task OnRun(CancellationToken serviceCancellationToken)
  {
    while (true)
    {
      serviceCancellationToken.ThrowIfCancellationRequested();

      var (source, stream, cancellationToken) = await WaitQueue!.Dequeue(serviceCancellationToken);

      CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceCancellationToken, cancellationToken);
      source.SetResult(Processor!.ProcessAsync(stream, cancellationToken).Select((data) =>
      {
        return new TranscriptBlock(data.Text, data.Language, (long)data.Start.TotalMilliseconds, (long)(data.End - data.Start).TotalMilliseconds);
      }, cancellationTokenSource.Token));
    }
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    string path = Manager.Server.Configuration.WhisperDatasetPath;

    if (!File.Exists(path))
    {
      Logger.Log(LogLevel.Info, "Dataset model for transcript generation does not exist. The server will attempt to download now.");
      using Stream stream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Medium, QuantizationType.NoQuantization, cancellationToken);

      string? parentPath = Path.GetDirectoryName(path);
      if (parentPath != null && !File.Exists(parentPath))
      {
        Directory.CreateDirectory(parentPath);
      }

      using FileStream writeStream = File.OpenWrite(path);
      await stream.CopyToAsync(writeStream, cancellationToken);
    }

    try { WaitQueue?.Dispose(); } catch { }
    if (File.Exists(path))
    {
      Processor = WhisperFactory.FromPath(path)
        .CreateBuilder()
        .WithLanguage("auto")
        .SplitOnWord()
        .Build();
      WaitQueue = new();
    }
  }

  protected override async Task OnStop(Exception? exception)
  {
    try
    {
      await (Processor?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
    finally
    {
      try { WaitQueue?.Dispose(); } catch { }

      Processor = null;
      WaitQueue = null;
    }
  }
}

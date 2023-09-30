namespace RizzziGit.EnderBytes;

using Collections;

public sealed partial class EnderBytesServer
{
  public abstract class ProtocolWrapper
  {
    private static ulong GenerateAndRegisterID(EnderBytesProtocolWrapper protocolWrapper, EnderBytesServer server)
    {
      ulong id;

      do
      {
        id = (ulong)Random.Shared.NextInt64();
      }
      while (!server.ProtocolWrappers.TryAdd(id, new(protocolWrapper)));

      return id;
    }

    public ProtocolWrapper(EnderBytesServer server)
    {
      Server = server;

      if (this is EnderBytesProtocolWrapper wrapper)
      {
        ID = GenerateAndRegisterID(wrapper, server);
      }
      else
      {
        throw new InvalidOperationException($"Must be inherited from {nameof(EnderBytesProtocolWrapper)} class.");
      }
    }

    ~ProtocolWrapper() => Server.ProtocolWrappers.Remove(ID);

    protected async Task<EnderBytesContext> GetContext(CancellationToken cancellationToken)
    {
      TaskCompletionSource<EnderBytesContext> source = new();
      (await Server.ListenQueue.Dequeue(cancellationToken)).SetResult((source, cancellationToken));
      return await source.Task;
    }

    public readonly EnderBytesServer Server;
    public readonly ulong ID;
  }

  private readonly Dictionary<ulong, WeakReference<EnderBytesProtocolWrapper>> ProtocolWrappers = [];
  private readonly WaitQueue<TaskCompletionSource<(TaskCompletionSource<EnderBytesContext> source, CancellationToken cancellationToken)>> ListenQueue = new(0);

  public async Task Listen(CancellationToken cancelationToken)
  {
    while (true)
    {
      cancelationToken.ThrowIfCancellationRequested();

      TaskCompletionSource<(TaskCompletionSource<EnderBytesContext> source, CancellationToken cancellationToken)> source = new();
      await ListenQueue.Enqueue(source, cancelationToken);
      var (remoteSource, remoteCancellationToken) = await source.Task;

      CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        cancelationToken,
        remoteCancellationToken
      );

      if (linkedCancellationTokenSource.Token.IsCancellationRequested)
      {
        remoteSource.SetCanceled(linkedCancellationTokenSource.Token);
        continue;
      }

      remoteSource.SetResult(new(this, linkedCancellationTokenSource.Token));
    }
  }
}

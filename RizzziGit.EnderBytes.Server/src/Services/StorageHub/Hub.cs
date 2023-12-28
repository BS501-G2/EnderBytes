namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Framework.Collections;
using RizzziGit.EnderBytes.Records;

public sealed partial class StorageHubService
{
  public enum NodeType : byte
  {
    File, Folder, SymbolicLink
  }

  [Flags]
  public enum FileAccess : byte
  {
    Read = 1 << 0,
    Write = 1 << 1,
    Exclusive = 1 << 2,

    ReadWrite = Read | Write,
    ExclusiveReadWrite = Exclusive | ReadWrite
  }

  [Flags]
  public enum FileMode : byte
  {
    TruncateToZero = 1 << 0,
    Append = 1 << 1
  }

  public abstract partial class Hub(StorageHubService service, long hubId, KeyService.Transformer.Key hubKey) : Lifetime($"{hubId}", service.Logger)
  {
    public static Hub StartHub(StorageHubService service, Record.StorageHub record, KeyService.Transformer.Key inputKey, CancellationToken cancellationToken)
    {
      Hub hub = record.Type switch
      {
        StorageHubType.Blob => new Blob(service, record.Id, inputKey),

        _ => throw new InvalidDataException("Unknown storage hub type.")
      };

      hub.Start(cancellationToken);
      return hub;
    }

    public Server Server => Service.Server;
    public readonly StorageHubService Service = service;
    public readonly long HubId = hubId;

    protected readonly KeyService.Transformer.Key HubKey = hubKey;

    private readonly WaitQueue<(TaskCompletionSource<Session> source, ConnectionService.Connection connection)> WaitQueue = new(0);
    private readonly WeakDictionary<ConnectionService.Connection, Session> Sessions = [];

    protected abstract Session Internal_NewSession(ConnectionService.Connection connection);

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      await foreach (var (source, connection) in WaitQueue.WithCancellation(cancellationToken))
      {
        try
        {
          if (!connection.IsRunning)
          {
            throw new InvalidOperationException("Connection is not active.");
          }

          if (!Sessions.TryGetValue(connection, out Session? session))
          {
            (Sessions[connection] = session = Internal_NewSession(connection)).Start(cancellationToken);
          }

          source.SetResult(session);
        }
        catch (Exception exception)
        {
          source.SetException(exception);
        }
      }
    }
  }
}

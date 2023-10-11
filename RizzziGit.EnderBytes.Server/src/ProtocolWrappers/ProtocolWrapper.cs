namespace RizzziGit.EnderBytes.ProtocolWrappers;

using Connections;
using Collections;

public abstract class ProtocolWrapper
{
  private static readonly WeakKeyDictionary<EnderBytesServer, Dictionary<int, ProtocolWrapper>> Servers = new();
  private Dictionary<int, ProtocolWrapper> Fellow
  {
    get
    {
      lock (Servers)
      {
        if (Servers.TryGetValue(Server, out var value))
        {
          return value;
        }

        Dictionary<int, ProtocolWrapper> list = [];
        Servers.TryAdd(Server, list);
        return list;
      }
    }
  }

  public static async Task RunProtocolWrappers(EnderBytesServer server, Func<CancellationToken, Task<ClientConnection>> getContextCallback, CancellationToken cancellationToken)
  {
    if (Servers.TryGetValue(server, out var value))
    {
      List<Task> tasks = [];

      foreach (ProtocolWrapper wrapper in value.Values)
      {
        tasks.Add(wrapper.Run(getContextCallback, cancellationToken));
      }

      await Task.WhenAll(tasks);
    }
  }

  protected ProtocolWrapper(string name, EnderBytesServer server)
  {
    Server = server;
    Dictionary<int, ProtocolWrapper> fellow = Fellow;
    do
    {
      ID = Random.Shared.Next();
    }
    while (!fellow.TryAdd(ID, this));

    Name = name;
    server.Logger.Subscribe(Logger = new($"({name}) Protocol"));
  }

  ~ProtocolWrapper()
  {
    Fellow.Remove(ID);
  }

  private readonly int ID;
  public readonly EnderBytesServer Server;

  public readonly string Name;
  public readonly Logger Logger;

  protected abstract Task OnRun(Func<CancellationToken, Task<ClientConnection>> getContextCallback, CancellationToken cancellationToken);
  private (Task task, CancellationTokenSource cancellationTokenSource)? RunTask;

  private async Task Run(Func<CancellationToken, Task<ClientConnection>> getContextCallback, CancellationToken cancellationToken)
  {
    CancellationTokenSource cancellationTokenSource = new();
    CancellationTokenSource joinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

    try
    {
      var task = RunTask ??= (OnRun(getContextCallback, cancellationToken), cancellationTokenSource);
      cancellationToken.Register(() => { try { task.cancellationTokenSource.Cancel(); } catch { } });

      await task.task;
    }
    finally
    {
      joinedCancellationTokenSource.Dispose();
      cancellationTokenSource.Dispose();

      RunTask = null;
    }
  }
}

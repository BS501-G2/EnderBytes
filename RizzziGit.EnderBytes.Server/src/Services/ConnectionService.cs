using System.Net;

namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;

public sealed partial class ConnectionService(Server server) : Server.SubService(server, "Connections")
{
  public abstract record EndPoint
  {
    private EndPoint() { }

    public sealed record Network(IPEndPoint Endpoint) : EndPoint;
    public sealed record Unix(string Path) : EndPoint;
    public sealed record Null() : EndPoint;

    public override abstract string ToString();
  }

  private interface IConnection : Connections.IConnection;
  private interface IConnectionConfiguration : Connections.IConnectionConfiguration;

  public abstract partial class Connection<C, CC> : IConnection
    where C : Connection<C, CC>
    where CC : Connection<C, CC>.ConnectionConfiguration
  {
    public abstract partial record ConnectionConfiguration();
  }

  private readonly WeakDictionary<long, IConnection> Connections = [];
  private readonly WaitQueue<(TaskCompletionSource<IConnection> Source, IConnectionConfiguration Configuration, CancellationToken CancellationToken)>? WaitQueue;

  // public Task<Connections.IConnection> GetConnection(CancellationToken cancellationToken = default)
  // {
  // }
}

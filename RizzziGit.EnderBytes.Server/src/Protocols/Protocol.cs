using System.Net;

namespace RizzziGit.EnderBytes.Protocols;

using Framework.Logging;

using Services;
using Extras;
using Utilities;

public abstract class Protocol<P, PC>(ProtocolService service, string name) : ProtocolService.Protocol(service, name)
  where P : Protocol<P, PC>
  where PC : Protocol<P, PC>.Connection
{
  public abstract class Connection(P protocol)
  {
    public readonly P Protocol = protocol;

    public abstract ConnectionEndPoint LocalEndPoint { get; }
    public abstract ConnectionEndPoint RemoteEndPoint { get; }

    public abstract Task Handle(CancellationToken cancellationToken);
  }

  protected abstract IAsyncEnumerable<PC> Listen(CancellationToken cancellationToken);

  private async void Handle(PC protocolConnection, CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Info, $"Client from {protocolConnection.RemoteEndPoint} connected.");

    try
    {
      await protocolConnection.Handle(cancellationToken);
    }
    catch (Exception exception)
    {
      if (!exception.IsDueToCancellationToken(cancellationToken))
      {
        Logger.Log(LogLevel.Warn, $"Client handler for {protocolConnection.RemoteEndPoint} has crashed: {exception.Stringify()}");

        return;
      }
    }

    Logger.Log(LogLevel.Info, $"Client handler for {protocolConnection.RemoteEndPoint} has stopped.");
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    Logger.Log(LogLevel.Info, $"Protocol handler for {Name} has started.");
    try
    {
      await foreach (PC protocolConnection in Listen(cancellationToken))
      {
        Handle(protocolConnection, cancellationToken);
      }
    }
    catch (Exception exception)
    {
      if (!exception.IsDueToCancellationToken(cancellationToken))
      {
        Logger.Log(LogLevel.Info, $"Protocol handler for {Name} has crashed: {exception.Stringify()}");

        throw;
      }
    }

    Logger.Log(LogLevel.Info, $"Protocol handler for {Name} has stopped.");
  }
}

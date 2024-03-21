using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;

public sealed partial class ClientService
{
  public sealed partial class UserClient : Client
  {
    public UserClient(WebSocket webSocket) : base()
    {
      Func<CancellationToken, Task>? exportStartFunction = null;
      WebSocket = new(this, webSocket, (startFunc) => exportStartFunction = startFunc);

      StartFunc = exportStartFunction!;
    }

    private readonly Func<CancellationToken, Task> StartFunc;
    private readonly ClientWebSocket WebSocket;

    public Task Handle(CancellationToken cancellationToken = default) => StartFunc(cancellationToken);

    private Task HandleMessage(CompositeBuffer message, CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }

    private Task<HybridWebSocket.Payload> HandleRequest(HybridWebSocket.Payload payload, CancellationToken cancellationToken = default)
    {
      return Task.FromResult<HybridWebSocket.Payload>(new(0, []));
    }

    private Task OnStart(CancellationToken cancellationToken = default)
    {
      return Task.CompletedTask;
    }
  }
}

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderBytes.Client.Library;

using Commons.Net;
using Commons.Memory;

public partial class Client
{
  public class WebSocketClient : HybridWebSocket
  {
    public WebSocketClient(Client client, ConnectionConfig config, WebSocket webSocket, Action<Func<CancellationToken, Task>> onStartFunc) : base(config, webSocket)
    {
      Client = client;

      onStartFunc(OnStart);
    }

    private readonly Client Client;

    protected override Task OnMessage(CompositeBuffer message, CancellationToken cancellationToken) => Client.HandleMessage(message, cancellationToken);
    protected override Task<Payload> OnRequest(Payload payload, CancellationToken cancellationToken) => Client.HandleRequest(payload, cancellationToken);
    protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  }
}

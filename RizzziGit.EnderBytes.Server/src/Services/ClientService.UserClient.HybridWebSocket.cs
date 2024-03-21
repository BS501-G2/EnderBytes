using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;

public sealed partial class ClientService
{
  public sealed partial class UserClient
  {
    public sealed class ClientWebSocket : HybridWebSocket
    {
      public ClientWebSocket(UserClient client, WebSocket webSocket, Action<Func<CancellationToken, Task>> startFunc) : base(new(), webSocket)
      {
        Client = client;

        startFunc(Start);
      }

      public readonly UserClient Client;

      protected override Task OnMessage(CompositeBuffer message, CancellationToken cancellationToken) => Client.HandleMessage(message, cancellationToken);
      protected override Task<Payload> OnRequest(Payload payload, CancellationToken cancellationToken) => Client.HandleRequest(payload, cancellationToken);
      protected override Task OnStart(CancellationToken cancellationToken) => Client.OnStart(cancellationToken);
    }
  }
}

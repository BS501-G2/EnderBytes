using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Utilities;

public sealed partial class ClientService(Server server) : Server.SubService(server, "Clients")
{
  public async Task HandleWebSocket(WebSocket webSocket)
  {
    UserClient client = new(webSocket);

    await client.Handle(GetCancellationToken());
  }
}

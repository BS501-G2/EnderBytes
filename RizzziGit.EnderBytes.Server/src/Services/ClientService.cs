using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;

using Core;
using Resources;

public sealed partial class ClientService(Server server) : Server.SubService(server, "Clients")
{
  private readonly WeakList<Client> Clients = [];

  public abstract partial class Client(ClientService service, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null)
  {
    public readonly ClientService Service = service;

    public UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken { get; protected set; } = userAuthenticationToken;
  }

  public async Task HandleUserClient(WebSocket webSocket, CancellationToken cancellationToken = default)
  {
    UserClient client = new(this);

    try
    {
      lock (Clients)
      {
        Clients.Add(client);
      }

      await client.Handle(webSocket, cancellationToken);
    }
    finally
    {
      lock (Clients)
      {
        Clients.Remove(client);
      }
    }
  }
}

using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Collections;

using Core;
using Resources;

public sealed partial class ClientService(Server server) : Server.SubService(server, "Clients")
{
  private readonly WeakList<Client> Clients = [];

  public abstract partial class Client
  {
    public readonly ClientService Service;

    public Client(ClientService service, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null)
    {
      Service = service;
      UserAuthenticationToken = userAuthenticationToken;
    }

    public UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken { get; protected set; }

    public FileResource GetRootFolder(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, UserAuthenticationToken!);
      FileResource rootFolder = transaction.GetManager<StorageResource.ResourceManager>().GetRootFolder(transaction, storage, UserAuthenticationToken!, cancellationToken);

      return rootFolder;
    }

    private FileResource GetTrashFolder(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, UserAuthenticationToken!);
      FileResource rootFolder = transaction.GetManager<StorageResource.ResourceManager>().GetTrashFolder(transaction, storage, UserAuthenticationToken!, cancellationToken);

      return rootFolder;
    }

    private FileResource GetInternalFolder(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, UserAuthenticationToken!);
      FileResource rootFolder = transaction.GetManager<StorageResource.ResourceManager>().GetInternalFolder(transaction, storage, UserAuthenticationToken!, cancellationToken);

      return rootFolder;
    }
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

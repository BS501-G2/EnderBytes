using System.Text;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;
using Resources;

public sealed partial class ClientService
{
  public sealed partial class RemoteUserClient : Client
  {
    public delegate ClientPayload Handler(ClientPayload requestPayload, ResourceService.Transaction transaction, CancellationToken cancellationToken = default);
    public delegate ClientPayload AuthenticatedHandler(UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, ClientPayload requestPayload, ResourceService.Transaction transaction, CancellationToken cancellationToken = default);

    public class RequestError(UserResponse response) : Exception
    {
      public readonly UserResponse Response = response;
    }

    private class HandlerDictionary(RemoteUserClient userClient) : Dictionary<UserRequest, Handler>
    {
      public readonly RemoteUserClient UserClient = userClient;

      public void Add(UserRequest request, AuthenticatedHandler authenticatedHandler) => Add(request, (request, transaction, cancellationToken) =>
      {
        UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken = UserClient.EnsureUserAuthenticationToken(transaction, cancellationToken);

        return authenticatedHandler(userAuthenticationToken, request, transaction, cancellationToken);
      });
    }

    public RemoteUserClient(ClientService service) : base(service)
    {
      Handlers = new(this);

      AddAllHandlers();
    }

    private readonly HandlerDictionary Handlers;

    public async Task Handle(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
      UserClient clientWebSocket = new(
        webSocket,
        (buffer) => Task.CompletedTask,
        (payload, cancellationToken) => Service.Server.ResourceService.Transact((ResourceService.Transaction transaction, CancellationToken cancellationToken) =>
        {
          ClientPayload requestPayload = ClientPayload.Deserialize(payload.Buffer);

          try
          {
            ClientPayload responsePayload = HandleRequest((UserRequest)payload.Code, requestPayload, transaction, CancellationToken.None);
            return new HybridWebSocket.Payload((uint)UserResponse.Okay, responsePayload.Serialize());
          }
          catch (Exception exception)
          {
            if (exception is RequestError requestException)
            {
              return new HybridWebSocket.Payload((uint)requestException.Response, exception.Message);
            }

            return new HybridWebSocket.Payload((uint)UserResponse.UnknownError, exception.Message);
          }
        }, cancellationToken)
      );

      await clientWebSocket.Start(cancellationToken);
    }

    private static T AsJson<T>(ClientPayload payload) where T : JToken
    {
      if (payload is not ClientPayload.JSON jsonPayload)
      {
        throw new RequestError(UserResponse.InvalidFormat);
      }

      return jsonPayload.AsJson<T>();
    }

    private static CompositeBuffer AsBytes<T>(ClientPayload payload)
    {
      if (payload is not ClientPayload.Raw rawPayload)
      {
        throw new RequestError(UserResponse.InvalidFormat);
      }

      return rawPayload.Buffer;
    }

    private UserAuthenticationResource.UserAuthenticationToken EnsureUserAuthenticationToken(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      if (
        (UserAuthenticationToken == null) ||
        transaction.GetManager<UserAuthenticationSessionTokenResource.ResourceManager>().GetByUserAuthentication(transaction, UserAuthenticationToken.UserAuthentication, cancellationToken).Expired
      )
      {
        throw new RequestError(UserResponse.InvalidCredentials);
      }

      return UserAuthenticationToken;
    }

    private ClientPayload HandleRequest(UserRequest request, ClientPayload requestPayload, ResourceService.Transaction transaction, CancellationToken cancellationToken = default) => Handlers.GetValueOrDefault(request, (_, _, _) => throw new RequestError(UserResponse.InvalidCommand))(requestPayload, transaction, cancellationToken);

    private void AddAllHandlers()
    {
      Handlers.Add(UserRequest.Echo, (request, _, _) => request);
      Handlers.Add(UserRequest.LoginPassword, (request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        string username = (string?)requestData["username"] ?? "";
        string password = (string?)requestData["password"] ?? "";

        if (
          !transaction.GetManager<UserResource.ResourceManager>().TryGetByUsername(transaction, username, out UserResource? user, cancellationToken) ||
          !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(password), UserAuthenticationResource.UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
        )
        {
          throw new RequestError(UserResponse.InvalidCredentials);
        }

        string token = transaction.GetManager<UserAuthenticationResource.ResourceManager>().CreateSessionToken(transaction, user, userAuthenticationToken);
        if (!transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, token, out UserAuthenticationResource.UserAuthenticationToken? sessionToken, cancellationToken))
        {
          throw new RequestError(UserResponse.InvalidCredentials);
        }

        UserAuthenticationToken = sessionToken;

        return new ClientPayload.JSON(new JObject
        {
          { "userId", user.Id },
          { "token", token }
        });
      });
      Handlers.Add(UserRequest.LoginToken, (request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        long userId = (long)requestData["userId"]!;
        string token = (string)requestData["token"]!;

        if (
          !transaction.GetManager<UserResource.ResourceManager>().TryGetById(transaction, userId, out UserResource? user, cancellationToken) ||
          !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, token, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, cancellationToken) ||
          transaction.GetManager<UserAuthenticationSessionTokenResource.ResourceManager>().GetByUserAuthentication(transaction, userAuthenticationToken.UserAuthentication, cancellationToken).Expired
        )
        {
          throw new RequestError(UserResponse.InvalidCredentials);
        }

        UserAuthenticationToken = userAuthenticationToken;

        return new ClientPayload.JSON(new JObject
        {
          { "userId", userId },
          { "token", token }
        });
      });
      Handlers.Add(UserRequest.Register, (request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        string username = (string)requestData["username"]!;
        string password = (string)requestData["password"]!;
        string lastName = (string)requestData["lastName"]!;
        string firstName = (string)requestData["firstName"]!;
        string? middleName = (string?)requestData["middleName"];

        (UserResource user, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken) = transaction.GetManager<UserResource.ResourceManager>().Create(transaction, username, lastName, firstName, middleName, password, cancellationToken);
        return new ClientPayload.JSON(JToken.FromObject(user.Id));
      });
      Handlers.Add(UserRequest.Logout, (userAuthenticationToken, _, transaction, cancellationToken) =>
      {
        transaction.GetManager<UserAuthenticationResource.ResourceManager>().Delete(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);
        UserAuthenticationToken = null;

        return new ClientPayload.JSON(JToken.Parse("null"));
      });
      Handlers.Add(UserRequest.GetOwnStorage, (userAuthenticationToken, request, transaction, cancellationToken) =>
      {
        StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);

        return new ClientPayload.JSON(JToken.FromObject(storage));
      });
      Handlers.Add(UserRequest.ResolveUserId, (_, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        string username = (string)requestData["username"]!;

        if (
          !transaction.GetManager<UserResource.ResourceManager>().TryGetByUsername(transaction, username, out UserResource? user, cancellationToken)
        )
        {
          return new ClientPayload.JSON("null");
        }

        return new ClientPayload.JSON(JToken.FromObject(user.Id));
      });
      Handlers.Add(UserRequest.GetUser, (userAuthenticationToken, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        long userId = (long)requestData["userId"]!;

        if (!transaction.GetManager<UserResource.ResourceManager>().TryGetById(transaction, userId, out UserResource? user, cancellationToken))
        {
          throw new RequestError(UserResponse.ResourceNotFound);
        }

        return new ClientPayload.JSON(JToken.FromObject(user));
      });
      Handlers.Add(UserRequest.ScanFolder, (userAuthenticationToken, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        List<long> ids = [];

        long? folderId = (long?)requestData["folderId"];

        if (folderId == null)
        {
          StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
          FileResource folder = transaction.GetManager<StorageResource.ResourceManager>().GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

          foreach (FileResource file in transaction.GetManager<FileResource.ResourceManager>().ScanFolder(transaction, storage, folder, userAuthenticationToken, cancellationToken))
          {
            ids.Add(file.Id);
          }
        }
        else
        {
          if (
            !transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, (long)folderId, out FileResource? folder, cancellationToken) ||
            !transaction.GetManager<StorageResource.ResourceManager>().TryGetById(transaction, folder.StorageId, out StorageResource? remoteStorage, cancellationToken)
          )
          {
            throw new RequestError(UserResponse.ResourceNotFound);
          }

          foreach (FileResource file in transaction.GetManager<FileResource.ResourceManager>().ScanFolder(transaction, remoteStorage, folder, userAuthenticationToken, cancellationToken))
          {
            ids.Add(file.Id);
          }
        }


        return new ClientPayload.JSON(JToken.FromObject(ids));
      });
      Handlers.Add(UserRequest.GetFile, (userAuthentication, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        long fileId = (long)requestData["fileId"]!;

        if (!transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, fileId, out FileResource? file, cancellationToken))
        {
          throw new RequestError(UserResponse.ResourceNotFound);
        }

        return new ClientPayload.JSON(JToken.FromObject(file));
      });

      Handlers.Add(UserRequest.Create, (userAuthenticationToken, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        string name = (string)requestData["name"]!;
        long? folderId = (long?)requestData["folderId"];
        bool isFolder = (bool)requestData["isFolder"]!;

        if (folderId == null)
        {
          StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
          FileResource folder = transaction.GetManager<StorageResource.ResourceManager>().GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

          folderId = folder.Id;
        }

        if (
          !transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, (long)folderId, out FileResource? parentFolder, cancellationToken) ||
          !transaction.GetManager<StorageResource.ResourceManager>().TryGetById(transaction, parentFolder.StorageId, out StorageResource? remoteStorage, cancellationToken)
        )
        {
          throw new RequestError(UserResponse.ResourceNotFound);
        }

        FileResource newFile = isFolder
          ? transaction.GetManager<FileResource.ResourceManager>().CreateFolder(transaction, remoteStorage, parentFolder, name, userAuthenticationToken, cancellationToken)
          : transaction.GetManager<FileResource.ResourceManager>().CreateFile(transaction, remoteStorage, parentFolder, name, userAuthenticationToken, cancellationToken);

        return new ClientPayload.JSON(JToken.FromObject(newFile.Id));
      });

      Handlers.Add(UserRequest.OpenFile, (userAuthenticationToken, request, transaction, cancellationToken) =>
      {
        JObject requestData = AsJson<JObject>(request);

        long fileId = (long)requestData["fileId"]!;
        long? baseSnapshotId = (long?)requestData["baseSnapshotId"];
        bool enableWrite = (bool)requestData["enableWrite"]!;

        FileSnapshotResource? baseSnapshot = null;

        if (
          (!transaction.GetManager<FileResource.ResourceManager>().TryGetById(transaction, fileId, out FileResource? file, cancellationToken)) ||

          (!transaction.GetManager<StorageResource.ResourceManager>().TryGetById(transaction, file.StorageId, out StorageResource? storage, cancellationToken))||
          (baseSnapshotId != null && (!transaction.GetManager<FileSnapshotResource.ResourceManager>().TryGetById(transaction, (long)baseSnapshotId, out baseSnapshot, cancellationToken)))
        )
        {
          throw new RequestError(UserResponse.ResourceNotFound);
        }

        FileResource.FileHandle fileHandle = transaction.GetManager<FileResource.ResourceManager>().OpenFile(transaction, storage, file, baseSnapshot, userAuthenticationToken, enableWrite ? FileResource.FileHandleFlags.ReadModify : FileResource.FileHandleFlags.Read, cancellationToken);

        return new ClientPayload.JSON(JToken.FromObject(fileHandle));
      });

      Handlers.Add(UserRequest.GetRootFolderId, (userAuthenticationToken, request, transaction, cancellationToken) => {
        StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
        FileResource rootFolder = transaction.GetManager<StorageResource.ResourceManager>().GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

        return new ClientPayload.JSON(JToken.FromObject(rootFolder.Id));
      });
    }
  }
}

using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Services;

using System.Text;
using Commons.Memory;
using Commons.Net;
using Newtonsoft.Json;
using RizzziGit.EnderBytes.Resources;

public sealed partial class ClientService
{
  public sealed partial class RemoteUserClient(ClientService service) : Client(service)
  {
    public delegate ClientPayload Handler(ClientPayload requestPayload, ResourceService.Transaction transaction, CancellationToken cancellationToken = default);

    public class RequestError(UserResponse response) : Exception
    {
      public readonly UserResponse Response = response;
    }

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
              return new HybridWebSocket.Payload((uint)requestException.Response, []);
            }

            return new HybridWebSocket.Payload((uint)UserResponse.UnknownError, []);
          }
        }, cancellationToken)
      );

      await clientWebSocket.Start(cancellationToken);
    }

    private readonly Handler HandleDefault = (_, _, _) => throw new RequestError(UserResponse.InvalidCommand);

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

    private ClientPayload HandleRequest(UserRequest request, ClientPayload requestPayload, ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      Handler handler = request switch
      {
        UserRequest.Echo => HandleEcho,
        UserRequest.LoginPassword => HandleLoginPassword,
        UserRequest.LoginToken => HandleLoginToken,
        UserRequest.Register => HandleRegister,
        UserRequest.Logout => HandleLogout,
        UserRequest.GetOwnStorageId => HandleGetOwnStorageId,

        _ => HandleDefault
      };

      return handler(requestPayload, transaction, cancellationToken);
    }

    private Handler HandleGetOwnStorageId => (request, transaction, cancellationToken) =>
    {
      UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken = EnsureUserAuthenticationToken(transaction, cancellationToken);
      StorageResource storage = transaction.GetManager<StorageResource.ResourceManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);

      return new ClientPayload.JSON(JToken.FromObject(storage));
    };

    private readonly Handler HandleEcho = (request, _, _) => request;
    private readonly Handler HandleLoginPassword = (request, transaction, cancellationToken) =>
    {
      JObject requestData = AsJson<JObject>(request);

      string username = (string)requestData["username"]!;
      string password = (string)requestData["password"]!;

      if (
        !transaction.GetManager<UserResource.ResourceManager>().TryGetByUsername(transaction, username, out UserResource? user, cancellationToken) ||
        !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(password), UserAuthenticationResource.UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
      )
      {
        throw new RequestError(UserResponse.InvalidCredentials);
      }

      string token = transaction.GetManager<UserAuthenticationResource.ResourceManager>().CreateSessionToken(transaction, user, userAuthenticationToken);
      return new ClientPayload.JSON(new JObject
      {
        { "userId", user.Id },
        { "token", token }
      });
    };

    private readonly Handler HandleLoginToken = (request, transaction, cancellationToken) =>
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

      return new ClientPayload.JSON(new JObject
      {
        { "userId", userId },
        { "token", token }
      });
    };

    private readonly Handler HandleRegister = (request, transaction, cancellationToken) =>
    {
      JObject requestData = AsJson<JObject>(request);

      string username = (string)requestData["username"]!;
      string password = (string)requestData["password"]!;
      string lastName = (string)requestData["lastName"]!;
      string firstName = (string)requestData["firstName"]!;
      string? middleName = (string?)requestData["middleName"];

      (UserResource user, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken) = transaction.GetManager<UserResource.ResourceManager>().Create(transaction, username, lastName, firstName, middleName, password, cancellationToken);
      return new ClientPayload.JSON(JToken.FromObject(user.Id));
    };

    private readonly Handler HandleLogout = (request, transaction, cancellationToken) =>
    {
      JObject requestData = AsJson<JObject>(request);

      long userId = (long)requestData["userId"]!;
      string token = (string)requestData["token"]!;

      if (
        !transaction.GetManager<UserResource.ResourceManager>().TryGetById(transaction, userId, out UserResource? user, cancellationToken) ||
        !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, token, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, cancellationToken)
      )
      {
        throw new RequestError(UserResponse.InvalidCredentials);
      }

      transaction.GetManager<UserAuthenticationResource.ResourceManager>().Delete(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);
      return new ClientPayload.JSON(JToken.Parse("null"));
    };
  }
}
